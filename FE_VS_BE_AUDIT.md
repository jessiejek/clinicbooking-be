# Frontend vs Backend API Endpoint Audit

Generated: 2026-05-27
Backend: clinicbooking-be (ASP.NET) — 20 controllers
Frontend: clinic_fe_dotnet (Angular/Ionic) — scanned 234 .ts files

---

## ✅ PART 1: ALL CORRECT — FE calls BE, matched 1:1

### AUTH
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| POST | /api/auth/login | `login.page.ts` |
| POST | /api/auth/register | `register.page.ts` |
| POST | /api/auth/google | `login.page.ts`, `register.page.ts` |
| POST | /api/auth/facebook | `login.page.ts`, `register.page.ts` |
| POST | /api/auth/refresh-token | `auth.interceptor.ts` |
| POST | /api/auth/logout | All layout components |
| GET | /api/auth/me | `app.config.ts`, `auth-callback.page.ts` |
| PUT | /api/auth/me | `staff-profile.page.ts` |
| POST | /api/auth/change-password | `doctor-profile.page.ts`, `patient-profile.page.ts`, `staff-profile.page.ts` |
| POST | /api/auth/set-password | `set-password.page.ts` |

### ANNOUNCEMENTS
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/announcements | `announcements.page.ts`, `home.page.ts` |
| POST | /api/announcements | `announcements.page.ts` |
| PUT | /api/announcements/{id} | `announcements.page.ts` |
| DELETE | /api/announcements/{id} | `announcements.page.ts` |

### AUDIT LOGS
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/audit-logs | `audit-logs.page.ts`, `doctor-consultation.page.ts` |
| POST | /api/audit-logs | `booking-detail.page.ts`, `doctor-consultation.page.ts` |

### BOOKINGS
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/bookings | `booking.service.ts`, `booking-detail.page.ts`, `walk-in.page.ts` |
| POST | /api/bookings | `booking.service.ts`, `step-payment`, `step-proof`, `walk-in.page.ts` |
| GET | /api/bookings/{id} | `booking-detail.page.ts` |
| PATCH | /api/bookings/{id}/check-in | `staff-booking-detail`, `staff-bookings`, `staff-dashboard` |
| PATCH | /api/bookings/{id}/undo-check-in | `staff-booking-detail`, `staff-bookings`, `staff-dashboard` |
| PATCH | /api/bookings/{id}/doctor-complete | `doctor-appointments`, `doctor-consultation` |
| GET | /api/bookings/{id}/consultation-record | `doctor-consultation.page.ts`, `doctor-patient-detail` |
| PATCH | /api/bookings/{id}/consultation-record | `doctor-consultation.page.ts` |
| PATCH | /api/bookings/{id}/confirm | `booking-detail.page.ts` |
| PATCH | /api/bookings/{id}/cancel | `booking-detail.page.ts`, `patient-booking-detail`, `patient-bookings` |
| PATCH | /api/bookings/{id}/complete | `booking-detail.page.ts` |
| PATCH | /api/bookings/{id}/no-show | `booking-detail.page.ts` |
| GET | /api/bookings/me | `patient-booking-detail`, `patient-dashboard`, `patient-bookings` |
| GET | /api/bookings/doctor/today | `doctor-appointments`, `doctor-dashboard` |
| GET | /api/bookings/doctor/today-summary | `doctor-appointments`, `doctor-dashboard` |
| GET | /api/bookings/doctor/patients | `doctor-patients.page.ts` |
| GET | /api/bookings/staff/all | `booking.service.ts`, `staff-bookings` |
| GET | /api/bookings/staff/today | `booking.service.ts` |
| GET | /api/bookings/staff/for-payment | `booking.service.ts`, `staff-payments` |

### DOCTORS
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/doctors | Multiple pages |
| GET | /api/doctors/admin | `doctor-form.page.ts`, `doctor-state.service.ts`, `services.page.ts` |
| GET | /api/doctors/{id} | Multiple pages |
| GET | /api/doctors/{id}/services | `walk-in.page.ts`, `booking-summary-bar`, `step-doctor-service`, `step-payment`, `step-review` |
| PUT | /api/doctors/{id} | `doctor-form.page.ts`, `doctors.page.ts` |
| GET | /api/doctors/me | `doctor-consultation.page.ts`, `doctor-dashboard`, `doctor-schedule` |
| PUT | /api/doctors/me | `doctor-profile.page.ts` |
| GET | /api/doctors/{id}/schedule | `doctor-form.page.ts`, `doctors.page.ts`, `doctor-dashboard`, `doctor-schedule`, `doctor-profile.page.ts` |
| PUT | /api/doctors/{id}/schedule | `doctor-form.page.ts`, `doctor-schedule.page.ts` |
| GET | /api/doctors/{id}/blocked-dates | `doctor-schedule.page.ts` |
| POST | /api/doctors/{id}/blocked-dates | `doctor-schedule.page.ts` |
| DELETE | /api/doctors/{id}/blocked-dates/{id} | `doctor-schedule.page.ts` |
| GET | /api/doctors/{id}/day-status | `doctor-dashboard.page.ts` |
| POST | /api/doctors/{id}/day-status | `doctor-state.service.ts`, `doctor-dashboard.page.ts`, `doctor-status.page.ts` |
| GET | /api/doctors/{id}/available-slots | `walk-in.page.ts`, `step-slot-select.component.ts` |

### SERVICES
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/services | `dashboard.page.ts`, `services.page.ts`, `walk-in`, `booking`, `booking-summary-bar`, `step-doctor-service` |
| POST | /api/services | `services.page.ts` |
| PUT | /api/services/{id} | `services.page.ts` |

### PATIENTS
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/patients | `patient-state.service.ts`, `booking-detail`, `patient-detail`, `doctor-consultation` |
| POST | /api/patients | `admin-patient-edit-modal`, `admin-patient-create-modal`, `walk-in.page.ts` |
| GET | /api/patients/{id} | `booking-detail.page.ts`, `doctor-patient-detail` |
| PUT | /api/patients/{id} | `patient-state.service.ts` |
| POST | /api/patients/{id}/portal-account | `patient-state.service.ts`, `staff-patient-detail` |
| GET | /api/patients/me | `patient-booking-detail`, `patient-dashboard`, `patient-reviews`, `step-proof` |
| PUT | /api/patients/me | `patient-profile.page.ts` |
| POST | /api/patients/me/consent | `patient-privacy-consent.page.ts`, `patient-profile.page.ts` |

### PATIENT DOCUMENTS & MEDIA
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/patients/{id}/documents | `patient-media-panel.component.ts` |
| POST | /api/patients/{id}/documents | `patient-media-panel.component.ts` (FormData) |
| GET | /api/patients/{id}/documents/{id}/file | `patient-media-panel.component.ts` (Blob) |
| GET | /api/patients/{id}/lab-results | `patient-media-panel.component.ts` |
| POST | /api/patients/{id}/lab-results | `patient-media-panel.component.ts` (FormData) |
| GET | /api/patients/{id}/lab-results/{id}/file | `patient-media-panel.component.ts` (Blob) |
| GET | /api/patients/{id}/clinical-history | `patient-detail.page.ts` |
| GET | /api/patients/{id}/vaccinations | Multiple pages |

### PATIENT-FACING DOCUMENTS (PDFs)
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/medical-records/me | `patient-medical-records.page.ts` |
| GET | /api/prescriptions/me | `patient-prescriptions.page.ts` |
| GET | /api/patient-documents/me/bookings/{id}/pdf | `patient-medical-records`, `patient-prescriptions` |
| GET | /api/patient-documents/me/prescriptions/{id}/pdf | `patient-prescriptions.page.ts` |
| GET | /api/patient-documents/me/medical-records/{id}/pdf | `patient-medical-records.page.ts` |
| GET | /api/patient-documents/me/all.pdf | `patient-layout`, `patient-medical-records`, `patient-prescriptions` |

### MEDICAL RECORDS (reads)
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/medical-records/consultations?patientId= | `admin/patient-detail`, `doctor/consultation`, `doctor/patient-detail`, `patient/dashboard` |
| GET | /api/medical-records/prescriptions?patientId= | Same pages |
| GET | /api/medical-records/allergies?patientId= | Same pages |
| GET | /api/medical-records/lab-orders?patientId= | `doctor/consultation` |
| GET | /api/medical-records/lab-results?patientId= | Same pages |
| GET | /api/medical-records/vaccinations?patientId= | Same pages |
| GET | /api/medical-records/follow-ups?patientId= | Same pages |
| POST | /api/medical-records/consultations | `doctor-consultation.page.ts` |
| POST | /api/medical-records/consultations/{id}/prescriptions | `doctor-consultation.page.ts` |

### PAYMENTS
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| PATCH | /api/payments/{id}/confirm | `staff-booking-detail`, `staff-payments` |
| PATCH | /api/payments/{id}/waive | `doctor-appointments`, `doctor-consultation`, `staff-booking-detail`, `staff-payments` |
| PATCH | /api/payments/{id}/refund | `booking-detail.page.ts` |

### STAFF / ADMIN
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| GET | /api/admin/staff | `staff.page.ts` |
| POST | /api/admin/staff/invite | `staff.page.ts` |
| PUT | /api/admin/staff/invite/{id}/revoke | `staff.page.ts` |
| PUT | /api/admin/staff/{id}/update-status | `staff.page.ts` |
| GET | /api/reports/unpaid-completed-visits | `reports.page.ts` |
| GET | /api/reports/pending-follow-ups | `reports.page.ts` |
| GET | /api/reports/daily-booking-summary | `reports.page.ts` |

### OTHER
| Method | Endpoint | Frontend Source |
|--------|----------|----------------|
| PUT | /api/notifications/{id}/read | `notification-panel.component.ts` |
| PUT | /api/notifications/read-all | `notification-panel.component.ts` |
| GET | /api/reviews?doctorId= | `doctor-profile.page.ts` |
| POST | /api/reviews | `patient-reviews.page.ts` |
| GET | /api/settings | `app.component.ts`, `home.page.ts` |
| PUT | /api/settings | `settings.page.ts` |
| POST | /api/drug-interactions/allergy-check | `prescription-form.component.ts` |
| POST | /api/drug-interactions/check | `prescription-form.component.ts` |

---

## ❌ PART 2: ISSUES — mismatches, missing, orphaned

### 🔴 ORPHANED — FE calls endpoint with NO backend route

| Method | Endpoint | Frontend File | Risk |
|--------|----------|---------------|------|
| GET | `medication_master` | `prescription-drug-list.ts` | 404 — no backend route exists |
| POST | `/consultation-requests/request-attending-physician` | `doctor-consultation.page.ts` | 404 — no backend route exists |

### 🟡 PARTIAL — has issues but works

| Method | Frontend calls | Backend expects | File | Issue |
|--------|---------------|----------------|------|-------|
| POST | `doctor-day-status/{id}/status` | `doctors/{id}/day-status` | `doctor-status.page.ts` | URL path doesn't match backend route |

### ❌ BACKEND ENDPOINTS — confirmed missing from FE scan

These endpoints exist on the backend but no frontend call was found. Likely either hidden in service wrappers or features not yet built.

| Method | Endpoint | Notes |
|--------|----------|-------|
| PATCH | /api/bookings/{id}/reschedule | Likely exists in service wrapper |
| POST | /api/bookings/{id}/proof | Patient proof submission |
| GET | /api/bookings/doctor/upcoming | Doctor feature not yet built |
| GET | /api/bookings/pending-verification | Staff verification flow |
| POST | /api/bookings/walk-in | Walk-in booking |
| PUT | /api/doctors/{id}/services | Update doctor service assignments |
| DELETE | /api/doctors/{id} | Delete doctor |
| GET | /api/services/{id} | Single service lookup |
| DELETE | /api/services/{id} | Delete service |
| POST | /api/device-tokens | Push notification registration |
| GET | /api/health | Health check |
| GET | /api/notifications | List notifications |
| GET | /api/admin/dashboard/summary | Admin dashboard stats |
| GET | /api/payments/booking/{id} | Payment lookup by booking |
| GET | /api/payments/{id}/receipt | Get receipt |
| GET | /api/follow-ups/me | Patient follow-ups |
| GET | /api/patients/me/documents | Patient my documents (list) |
| POST | /api/patients/me/documents | Patient my documents (upload) |
| GET | /api/patients/me/lab-results | Patient my lab results |
| POST | /api/patients/me/lab-results | Patient my lab results (upload) |
| GET | /api/patients/me/vaccinations | Patient my vaccinations |
| POST | /api/patients/{id}/vaccinations | Add patient vaccination |
| PUT | /api/patients/{id}/vaccinations/{id} | Update vaccination |
| DELETE | /api/patients/{id}/vaccinations/{id} | Delete vaccination |
| All remaining medical-records CRUD endpoints | (consultations CRUD, allergies CRUD, lab CRUD, vaccinations CRUD, follow-ups CRUD — ~20 endpoints) |

---

## SUMMARY

| Status | Count |
|--------|-------|
| ✅ Matched 1:1 | ~95 |
| 🔴 Orphaned (FE calls no BE) | 2 |
| 🟡 Partial mismatch | 1 |
| ❌ BE exists, no FE call found | ~35 (mostly CRUD in wrappers or not built) |
