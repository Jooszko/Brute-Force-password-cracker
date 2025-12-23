# 🔐 Brute Force ZIP Password Cracker

A desktop application written in **C# (WPF, MVVM)** for testing and analyzing the security of password-protected ZIP archives using **brute force** and **dictionary-based** methods.  
This project is intended for **educational and demonstration purposes only**.

---

## 📌 Features

- 🔓 ZIP archive password cracking
- 🔁 Two brute force approaches:
  - **Iterative** (multi-threaded)
  - **Recursive** (multi-threaded)
- 📚 Dictionary-based attack (wordlist support)
- 🧩 Custom character rules per password position
- 🔤 Supported character sets:
  - Lowercase letters
  - Uppercase letters
  - Numbers
  - Special characters
- 📏 Configurable password length range (MIN / MAX)
- ✍️ Manual password verification
- 🧵 Multi-core CPU utilization
- 📝 Real-time logging of password attempts

---

## 🖥️ Technologies

- **C#**
- **.NET (WPF)**
- **MVVM**
- **Ionic.Zip (DotNetZip)**
- **Multithreading** (`Task`, `Parallel`, `Interlocked`)
- **XAML**

---

## 📂 File Support

- Supported format: **`.zip`**
- The archive must contain **at least one encrypted file**

---

## ⚙️ Modes of Operation

### 🔑 Manual Password Verification
Allows users to enter a password manually to check if it is correct.

### 🔁 Brute Force – Iterative
- Iterative password generation
- Search space split across multiple threads

### 🔂 Brute Force – Recursive
- Recursive combination generation
- Parallel processing of the first-level search space

---

## 🧩 Character Rules (RuleText)

Custom rules can be defined for each password position using tokens:

| Token | Meaning |
|------|--------|
| `*` | Lowercase letters |
| `&` | Uppercase letters |
| `!` | Digits |
| `#` | Special characters |

---

## 🧠 Performance

- Automatically detects available CPU cores
- Utilizes parallel processing and thread synchronization
- Terminates all active threads immediately after the password is found

---

## 🚧 Development Plans

Planned improvements for future versions include:

### 🧪 Automated Testing
- Unit tests for brute force logic
- Performance and scalability testing of algorithms

### 📚 Enhanced Dictionary Handling
- Support for large wordlists using streaming
- Dictionary file selection directly from the UI
- Dictionary splitting based on password length

### 🐳 Docker
- Application containerization (CLI version)
- Running brute force tests in containerized environments
- Docker image preparation for CI/CD pipelines

---

## 👥 Authors

- **Arkadiusz Hebda** – Rauhvin  
- **Dominik Mikulski** – Leywin23 
- **Jonasz Kubaczka** – Jooszko
  
## 📷 Screenshots 
<img width="675" height="454" alt="image" src="https://github.com/user-attachments/assets/b27a51d6-0cff-4484-b68b-29b949593451" />


