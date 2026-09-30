#!/usr/bin/env bash
#
# Verifica que a lista PACKAGES do workflow de release (.github/workflows/tooark.yml) cobre todos os projetos
# empacotáveis da solução.
#
# A publicação no NuGet empacota só os projetos dessa lista. Um pacote novo que fique de fora não é publicado,
# e quem depende dele (o agregador Tooark, um provedor) sai com uma dependência que não existe no nuget.org.
# Este script confere que:
#   1. todo .csproj versionado sem <IsPackable>false</IsPackable> está na lista;
#   2. todo nome da lista corresponde a um projeto empacotável (<nome>/<nome>.csproj).
#
# O CI executa este script em todo pull request; localmente: bash scripts/check-release-packages.sh

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKFLOW="$ROOT/.github/workflows/tooark.yml"

# Nomes da lista PACKAGES: as linhas indentadas logo abaixo de "PACKAGES: >-", até a primeira linha que não seja
# um nome de projeto.
listed=$(awk '
  /^[[:space:]]*PACKAGES:[[:space:]]*>-/ { inside = 1; next }
  inside && /^[[:space:]]+[A-Za-z0-9.]+[[:space:]]*$/ { gsub(/[[:space:]]/, ""); print; next }
  inside { exit }
' "$WORKFLOW" | sort)

if [ -z "$listed" ]; then
  echo "ERRO: não encontrei a lista PACKAGES em $WORKFLOW" >&2
  exit 1
fi

# Projetos empacotáveis: <pasta>/<pasta>.csproj sem IsPackable=false.
packable=$(cd "$ROOT" && git ls-files '*.csproj' | while read -r csproj; do
  if ! grep -q '<IsPackable>false</IsPackable>' "$csproj"; then
    basename "$csproj" .csproj
  fi
done | sort)

status=0

missing=$(comm -13 <(echo "$listed") <(echo "$packable"))
unknown=$(comm -23 <(echo "$listed") <(echo "$packable"))

if [ -n "$missing" ]; then
  echo "ERRO: projetos empacotáveis fora da lista PACKAGES do tooark.yml (não seriam publicados):" >&2
  echo "$missing" | sed 's/^/  - /' >&2
  status=1
fi

if [ -n "$unknown" ]; then
  echo "ERRO: nomes da lista PACKAGES do tooark.yml sem projeto empacotável correspondente:" >&2
  echo "$unknown" | sed 's/^/  - /' >&2
  status=1
fi

if [ "$status" -eq 0 ]; then
  echo "OK: a lista PACKAGES do tooark.yml cobre os $(echo "$packable" | wc -l | tr -d ' ') projetos empacotáveis."
fi

exit "$status"
