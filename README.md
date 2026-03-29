## 🔧 Configuration Required

This project requires a local `appsettings.json` file, which is not included in the repository for security reasons.

### 📁 Create the file

In each project folder (`EWebsite/`, `WorkerService/`), create a file named:

```
appsettings.json
```

---

### ⚙️ Required configuration

Add the following structure and update values according to your environment:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "Admin": {
    "PrimaryAdminIdentifier": "your-email@example.com"
  },
  "ResetLog": {
    "Folder": "Path to your Logs folder"
  }
}
```

---

### 🗄 Database setup

Make sure SQL Server (SQLEXPRESS) is running:

1. Press `Win + R`
2. Type `services.msc`
3. Start **SQL Server (SQLEXPRESS)**

---

### ⚠️ Notes

* `appsettings.json` is ignored by Git and must not be committed
* Each developer must create their own local configuration
* Update paths and connection strings based on your system
