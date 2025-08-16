## 🔐 Secure Data Exchange App (RC5 & MARS Encryption)

A cryptographic application for secure file and message exchange, featuring modern encryption algorithms (RC5, MARS), flexible encryption modes, and scalable infrastructure.

---

## 🚀 Features
- File and text encryption with selectable algorithms (RC5, MARS)
- Support for multiple encryption modes: CBC, ECB, PCBC, CTR, RD, OFB, CFB
- Secure key exchange via Diffie–Hellman protocol with caching in Redis
- Asynchronous message processing with Apache Kafka
- CI/CD pipeline implemented in GitHub Actions
- User-friendly interface with support for PDFs and images
- Containerized deployment with Docker for scalability

---

## 🛠 Tech Stack
- **Languages:** C#, JavaScript
- **Backend:** ASP.NET Core Web API
- **Frontend:** React 
- **Database:** PostgreSQL 
- **Messaging:** Apache Kafka 
- **Caching:** Redis 
- **DevOps:** Docker, GitHub Actions

---

## ⚙️ Installation & Usage
Clone the repository:

```bash
git clone https://github.com/oduvanchikm/CryptographyChat.git
cd CryptographyChat
```

Run with Docker:

```bash
docker-compose up --build
```

---

## 📂 Project Structure
```bash
CryptographyChat/
│── Cryptography/           # Cryptographic algorithms implementation  
│── CryptographyChat.Tests/ # Tests  
│── SecureChat.Broker/      # Kafka configuration  
│── SecureChat.Common/      # Shared models (DB entities, ViewModels)  
│── SecureChat.Database/    # Database setup and migrations  
│── SecureChat.Server/      # Core application logic (backend)  
│── frontend/               # Frontend (React application)  
└── README.md               # Project overview
```

## ✅ Tests
```bash
dotnet test
```

## 👤 Author
oduvanchikm

