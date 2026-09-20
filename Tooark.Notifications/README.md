# Tooark.Notifications

Library for creating and managing notifications and alerts, easing communication and monitoring in .NET projects.

🌍 **Languages:** 🇺🇸 **English (this file)** · [🇧🇷 Português](https://github.com/Tooark/nuget-tooark/blob/main/Tooark.Notifications/README.pt-BR.md)

## Contents

- [Installation](#-installation)
- [NotificationItem](#1-notification-item)
- [Notification](#2-notification)
- [NotificationErrorMessages](#3-error-messages)

## 🔧 Installation

```bash
dotnet add package Tooark.Notifications
```

The package has no configuration. `Notification` is used through inheritance, in classes that accumulate
the result of several checks — such as the Tooark value objects and entities —, and the mutators are
protected: the object itself accumulates the notification, not whoever consumes it.

Messages are stored as a **key**, not as final text. Translation is done by the layer that builds the
response, such as the `ResponseDto` of [`Tooark.Dtos`](https://www.nuget.org/packages/Tooark.Dtos).

## Classes

The available classes are:

### 1. Notification Item

**Purpose:**
Represents the structure of a notification item with message, key and error code. The values are normalized on construction and the instance is read-only.

**Properties:**

- `Message`: The notification message.
- `Key`: The notification key.
- `Code`: The notification error code.

**Methods:**

- `NotificationItem(string? message)`: Creates a new instance with the `Unknown` key and the `T.ERR` code.
- `NotificationItem(string? message, string? key)`: Creates a new instance with the given key and the `T.ERR` code.
- `NotificationItem(string? message, string? key, string? code)`: Creates a new instance with the given key and code.

The three parameters accept a missing value because the constructor already handled it: a blank message
becomes `Notifications.MessageNullEmpty`, a blank key becomes `Unknown` and a blank code becomes `T.ERR`.
The signature declared non-nullable while the body checked for null — whoever passed a `string?` got a
compiler warning for a path that always worked.

- `ToString()`: Returns the notification message.
- `string`: Implicitly converts a `NotificationItem` instance to a string, returning the message. A null instance returns an empty string.
- `NotificationItem`: Implicitly converts a string to a `NotificationItem` instance, with the `Unknown` key and the `T.ERR` code.

**Value normalization:**

| Parameter | Null, empty or blank             | Other values                                           |
| --------- | -------------------------------- | ------------------------------------------------------ |
| `message` | `Notifications.MessageNullEmpty` | Leading and trailing whitespace removed                |
| `key`     | `Unknown`                        | All whitespace removed, including tabs and line breaks |
| `code`    | `T.ERR`                          | Leading and trailing whitespace removed                |

[**Usage Example**](#notification-item)

### 2. Notification

**Purpose:**
Abstract class that manages the notification list of the object inheriting it. It is the base of `ValueObject`, `BaseEntity` and `Validation`.

Creating and clearing notifications is protected: only the object itself decides what it notifies. Aggregating existing notifications is public, allowing the validation result of other objects to be composed.

**Properties:**

- `Notifications`: Returns the read-only list of notifications.
- `IsValid`: Returns true if the notification list is empty.
- `Count`: Returns the number of notifications.
- `Codes`: Returns the list of error codes of the notifications.
- `Keys`: Returns the list of keys of the notifications.
- `Messages`: Returns the list of messages of the notifications.

**Public methods:**

- `AddNotifications(Notification notification)`: Adds the notifications of another notification to the notification list.
- `AddNotifications(params Notification[] notifications)`: Adds the notifications of a collection of notifications to the notification list.

**Protected methods:**

- `AddNotification(NotificationItem notification)`: Adds a notification item to the notification list.
- `AddNotification(Type property, string message)`: Adds a notification item using the type name as key.
- `AddNotification(Type property, string message, string code)`: Adds a notification item using the type name as key, with an error code.
- `AddNotification(string message, string key)`: Adds a notification item with message and key.
- `AddNotification(string message, string key, string code)`: Adds a notification item with message, key and error code.
- `AddNotifications(ICollection<NotificationItem> notifications)`: Adds a collection of notification items to the notification list.
- `Clear()`: Clears the notification list.

**Null handling:**

| Situation                                                | Behavior                                               |
| -------------------------------------------------------- | ------------------------------------------------------ |
| Null argument in `AddNotification` or `AddNotifications` | Adds the `Notifications.NotificationNull` notification |
| Null item inside a collection                            | Ignored, without changing the list                     |
| Adding the instance itself in `AddNotifications`         | No effect, the list is not changed                     |

[**Usage Example**](#notification)

### 3. Error Messages

**Purpose:**
Static class `NotificationErrorMessages` with the messages generated by the library itself. They are translation keys, not final texts.

| Constant               | Value                            | When it occurs                                          |
| ---------------------- | -------------------------------- | ------------------------------------------------------- |
| `MessageIsNullOrEmpty` | `Notifications.MessageNullEmpty` | Null, empty or whitespace-only message                  |
| `NotificationIsNull`   | `Notifications.NotificationNull` | Notification or collection received as argument is null |

## Usage Example

### Notification Item

```csharp
// Message, with the 'Unknown' key and the 'T.ERR' code
var notification1 = new NotificationItem("Example message");

// Message and key, with the 'T.ERR' code
var notification2 = new NotificationItem("Example message", "ExampleKey");

// Message, key and code
var notification3 = new NotificationItem("Example message", "ExampleKey", "XPTO1");

// The key has its whitespace removed
var notification4 = new NotificationItem("Example message", "Example Key");
var key = notification4.Key; // "ExampleKey"
```

### Notification

```csharp
// Adding notifications is protected: only the object itself notifies its errors
public class MyNotification : Notification
{
  public void NotifyError(string message)
  {
    AddNotification(message, "Error", "XPTO1");
  }
}

var myNotification = new MyNotification();

myNotification.NotifyError("An error occurred.");

var isValid = myNotification.IsValid; // False
var count = myNotification.Count; // 1
var codes = myNotification.Codes; // ["XPTO1"]
var keys = myNotification.Keys; // ["Error"]
var messages = myNotification.Messages; // ["An error occurred."]
var notifications = myNotification.Notifications; // [NotificationItem { Message = "An error occurred.", Key = "Error", Code = "XPTO1" }]
```

```csharp
// Aggregation is public: existing notifications can be composed from outside the object
public class MyValidation : Notification
{ }

var validation = new MyValidation();

validation.AddNotifications(myNotification, myOtherNotification);
```

## Contributing

Contributions are welcome! Feel free to open issues and pull requests in the [Tooark.Notifications](https://github.com/Tooark/nuget-tooark/issues) repository.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](https://raw.githubusercontent.com/Tooark/nuget-tooark/refs/heads/main/LICENSE) file for details.
