# 🏥 Tabiby

> A comprehensive healthcare platform that connects **Patients**, **Doctors**, **Pharmacies**, and **Home Nurses** in one unified system.

## 📖 Overview

Tabiby is an online healthcare platform designed to simplify healthcare services by allowing patients to:

- Book doctor appointments
- Order medications from pharmacies
- Request home nursing services
- Receive real-time notifications
- Chat with healthcare providers
- Manage their medical journey from one place

The project is built using **ASP.NET Core**, follows **Clean Architecture**, and implements modern software engineering practices.

---
# ✨ Features

## 👤 Authentication & Authorization

- JWT Authentication
- ASP.NET Core Identity
- Role-based Authorization
- Signal R for Real time
### Roles

- Patient
- Doctor
- Pharmacy
- Nurse
- Admin
---
## 👨‍⚕️ Doctor Module

- Doctor Profiles
- Available Schedules
- Appointment Booking
- Appointment Confirmation
- Appointment Cancellation
- Patient History

---

## 💊 Medication Module

- Browse Medications
- Search Medications
- Add to Basket
- Update Basket
- Remove from Basket
- Checkout
- Order Management

---

## 🛒 Basket Module

- Add Medication
- Update Quantity
- Remove Item
- Calculate Total Price

---

## 📦 Order Module

- Create Orders
- Track Order Status
- Cancel Orders
- Restore Stock on Cancellation

---

## 👩‍⚕️ Nursing Module

- Browse Nurses
- Request Home Nursing
- Manage Nursing Requests

---

## 💬 Chat Module

- Real-time Messaging
- SignalR Integration

---

## 🔔 Notification Module

Real-time notifications for:

- Appointment Updates
- Order Updates
- New Messages
- Nursing Requests

Implemented using **SignalR**.

---

# 🏗️ Architecture

The project follows **Clean Architecture**.

```
Presentation Layer
        │
Business Logic Layer (BLL)
        │
Data Access Layer (DAL)
        │
Database
```


# 🛠️ Technologies

- ASP.NET Core 9
- Entity Framework Core
- SQL Server
- ASP.NET Identity
- JWT Authentication
- SignalR
- AutoMapper
- Swagger
- Clean Architecture
- Repository Pattern
- Unit of Work Pattern
- Specification Pattern
- Result Pattern

---
# 🔐 Authentication

Authentication is implemented using:

- JWT Bearer Token
- ASP.NET Identity

Protected endpoints require a valid JWT token.

---

# 📡 Real-Time Communication

SignalR is used for:

- Chat
- Notifications

---

# 🗄️ Database

Database Engine:

- SQL Server

ORM:

- Entity Framework Core

Features:

- Code First
- Migrations
- Seed Data

---
