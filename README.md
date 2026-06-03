# 📚 BookBridge — Pakistan's Premier Book Marketplace

> **Production-quality ASP.NET Core MVC web application** for buying, selling, borrowing, donating, and exchanging books with real-time chat, secure payments, and a modern glassmorphism UI.

---

## 🌟 Overview

BookBridge is a full-featured book ecosystem built as a Final Year Project / Startup MVP. It combines modern backend engineering with a premium, Apple-inspired UI to create an experience that rivals real-world SaaS products.

---

## 🚀 Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core MVC (.NET 8), C# |
| **ORM** | Entity Framework Core 8 |
| **Database** | SQL Server (LocalDB for dev) |
| **Auth** | ASP.NET Core Identity + Roles |
| **Real-Time** | SignalR (Chat + Notifications) |
| **Images** | SixLabors.ImageSharp (resizing) |
| **Frontend** | Bootstrap 5, GSAP 3, Bootstrap Icons |
| **Fonts** | Syne (display) + DM Sans (body) |
| **Design** | Glassmorphism, Gradient overlays |

---

## ✨ Features

### 🔐 Authentication
- Register / Login / Forgot Password / Reset Password
- Role-based authorization (Admin, User, Seller, Buyer, Donor, Borrower)
- Remember Me + Account lockout
- Email verification support

### 📚 Book Management
- List books with up to **5 photos** (drag & drop upload)
- Transaction types: **Sell, Borrow, Donate, Exchange**
- Book conditions, categories, language, city filters
- Featured book system (Admin-controlled)
- View counter

### ⏱️ Borrowing System
- Owner approves/rejects borrow requests
- **Real-time countdown timer** showing days/hours/minutes remaining
- Overdue detection with alerts
- Auto-marks book as unavailable while borrowed
- Return confirmation flow

### 💬 Real-Time Chat
- SignalR-powered instant messaging
- Typing indicators, read receipts
- Online/offline status
- Image sharing in chat
- Conversation history per book

### 🔔 Notification System
- Real-time toast notifications via SignalR
- Notification center with unread count badge
- Email notification architecture (ready for SMTP)

### ⭐ Reviews & Ratings
- 1–5 star rating system with interactive star picker
- Written reviews for sellers/borrowers
- Auto-computed average rating on user profile
- Review history on public profiles

### 🛡️ Admin Panel
- Full user management (ban/unban, promote to admin)
- Book management (feature/remove)
- Support ticket system with reply functionality
- Report management (resolve/dismiss)
- Category management
- Transaction monitoring
- Live analytics dashboard

### 👤 User Profiles
- Public profile with books + reviews
- CNIC-based identity verification
- Wallet balance display
- Activity history

### 🎫 Support System
- Submit and track support tickets
- Admin reply system
- Contact page integration

---

## 🗂️ Project Structure

```
BookBridge/
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── BooksController.cs
│   ├── Controllers.cs          ← Dashboard, Chat, Admin
├── Models/
│   ├── Entities/               ← EF Core domain models
│   ├── ViewModels/             ← Strongly-typed view models
├── Services/
│   ├── Interfaces/             ← Service contracts
│   ├── BookService.cs
│   ├── Services.cs             ← All other services
├── Hubs/
│   └── ChatHub.cs             ← SignalR hubs
├── Data/
│   └── ApplicationDbContext.cs ← EF Core DbContext + seeds
├── Views/
│   ├── Home/                   ← Public pages
│   ├── Account/                ← Auth pages
│   ├── Books/                  ← Book CRUD + browse
│   ├── Dashboard/              ← User dashboard + profile
│   ├── Chat/                   ← Messaging interface
│   ├── Admin/                  ← Admin panel
│   └── Shared/                 ← Layouts + partials
├── wwwroot/
│   ├── css/site.css            ← Complete design system
│   ├── js/site.js              ← GSAP animations + interactions
│   └── js/notifications.js     ← SignalR client
└── Program.cs                  ← DI + middleware + seeding
```

---

## ⚙️ Setup Instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code

### 1. Clone & Configure

```bash
git clone <your-repo>
cd BookBridge
```

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookBridgeDb;Trusted_Connection=True;"
  }
}
```

### 2. Install Packages

```bash
dotnet restore
```

### 3. Apply Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> The app auto-migrates and seeds on first startup, including the admin user and 10 book categories.

### 4. Run

```bash
dotnet run
```

Navigate to `https://localhost:5001`

---

## 🔑 Default Admin Credentials

| Field | Value |
|-------|-------|
| **Email** | admin@bookbridge.com |
| **Password** | Admin@123456 |

> ⚠️ Change this immediately in production!

---

## 🗄️ Database Schema

The database includes **15 tables** with proper relationships:

```
Users (AspNetUsers)     → Extended ApplicationUser
Books                   → Core book listings
BookImages              → Multiple photos per book
Categories              → 10 seeded categories
BorrowRequests          → Full borrow lifecycle
ExchangeRequests        → Exchange offers
Transactions            → Payment records
Reviews                 → Star ratings + comments
Conversations           → Chat threads
Messages                → Individual messages
Notifications           → Real-time alerts
SupportTickets          → Help desk
Reports                 → Fraud/abuse reports
```

---

## 🎨 Design System

The custom CSS design system uses CSS custom properties for full theming:

```css
--primary: #2563EB      /* Deep Blue */
--indigo:  #4F46E5      /* Indigo */
--emerald: #10B981      /* Emerald */
--amber:   #F59E0B      /* Amber */
--rose:    #F43F5E      /* Rose */
```

**Components include:** glassmorphism cards, floating ambient orbs, animated book cards with 3D hover, countdown timers, star rating pickers, drag-and-drop upload zones, real-time chat bubbles, SaaS-style dashboard analytics, and a full admin sidebar.

---

## 📦 Key NuGet Packages

```xml
Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.0
Microsoft.EntityFrameworkCore.SqlServer 8.0.0
Microsoft.AspNetCore.SignalR 1.1.0
SixLabors.ImageSharp 3.1.0
Stripe.net 43.0.0
MailKit 4.3.0
Serilog.AspNetCore 8.0.0
```

---

## 🔒 Security Features

- Anti-forgery tokens on all forms
- Input validation (server-side + model binding)
- Identity lockout (5 failed attempts → 5 min lock)
- Ownership validation (users can only edit their own books)
- Role-based authorization on admin routes
- SQL injection prevention via EF Core parameterized queries
- XSS protection via Razor HTML encoding
- CNIC verification for identity accountability

---

## 🗺️ Roadmap / Future Enhancements

- [ ] Stripe payment integration (architecture ready)
- [ ] Email notifications via MailKit/SMTP
- [ ] Escrow payment holding
- [ ] Mobile app (React Native / MAUI)
- [ ] AI book recommendations
- [ ] Google Maps integration for meetup points
- [ ] Advanced analytics charts (Chart.js)
- [ ] PWA support

---

## 📄 License

This project was created for educational purposes as a Final Year Project.  
© 2025 BookBridge. All rights reserved.

---

## 🤝 Contributing

Pull requests welcome. For major changes, open an issue first.

```
Built with ❤️ for Pakistan's reading community
```

