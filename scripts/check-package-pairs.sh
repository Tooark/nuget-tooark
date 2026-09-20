#!/usr/bin/env bash
#
# Verifica os pares de pins atrelados ao runtime no Directory.Packages.props.
#
# Os pacotes que acompanham a versão do .NET (Microsoft.AspNetCore.Authentication.*, Microsoft.Extensions.*,
# EF Core, ...) têm um pin por target framework, em dois ItemGroups condicionais. Este script confere que:
#   1. todo pacote do grupo net8.0 existe no grupo net10.0, e vice-versa;
#   2. cada versão pertence à linha do seu grupo (8.x no grupo net8.0, 10.x no grupo net10.0).
#
# Ele NÃO sabe se existe uma versão mais nova no nuget.org: subir os dois lados continua sendo tarefa manual.
# O CI executa este script em todo pull request; localmente: bash scripts/check-package-pairs.sh

set -euo pipefail

FILE="$(cd "$(dirname "$0")/.." && pwd)/Directory.Packages.props"

# Imprime "Id Versao" de cada PackageVersion dentro do ItemGroup condicionado ao TFM informado.
group() {
  sed -n "/Condition=\"'\$(TargetFramework)' == '$1'\"/,/<\/ItemGroup>/p" "$FILE" \
    | grep -o '<PackageVersion Include="[^"]*" Version="[^"]*"' \
    | sed -E 's/.*Include="([^"]*)" Version="([^"]*)".*/\1 \2/' \
    | sort
}

net8=$(group net8.0)
net10=$(group net10.0)

if [ -z "$net8" ] || [ -z "$net10" ]; then
  echo "ERRO: não encontrei os dois ItemGroups condicionais (net8.0 e net10.0) em $FILE" >&2
  exit 1
fi

status=0

only8=$(comm -23 <(echo "$net8" | cut -d' ' -f1) <(echo "$net10" | cut -d' ' -f1))
only10=$(comm -13 <(echo "$net8" | cut -d' ' -f1) <(echo "$net10" | cut -d' ' -f1))

if [ -n "$only8" ]; then
  echo "ERRO: presentes só no grupo net8.0 (falta o par no grupo net10.0):" >&2
  echo "$only8" | sed 's/^/  - /' >&2
  status=1
fi

if [ -n "$only10" ]; then
  echo "ERRO: presentes só no grupo net10.0 (falta o par no grupo net8.0):" >&2
  echo "$only10" | sed 's/^/  - /' >&2
  status=1
fi

bad8=$(echo "$net8" | awk '$2 !~ /^8\./ { print "  - " $1 " " $2 }')
bad10=$(echo "$net10" | awk '$2 !~ /^10\./ { print "  - " $1 " " $2 }')

if [ -n "$bad8" ]; then
  echo "ERRO: versões fora da linha 8.x no grupo net8.0:" >&2
  echo "$bad8" >&2
  status=1
fi

if [ -n "$bad10" ]; then
  echo "ERRO: versões fora da linha 10.x no grupo net10.0:" >&2
  echo "$bad10" >&2
  status=1
fi

if [ "$status" -eq 0 ]; then
  count=$(echo "$net8" | wc -l | tr -d ' ')
  echo "OK: $count pacotes atrelados ao runtime, em pares net8.0/net10.0 e nas linhas corretas."
fi

exit "$status"
