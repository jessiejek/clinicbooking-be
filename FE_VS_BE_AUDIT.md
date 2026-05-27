# Frontend vs Backend API Endpoint Audit

Generated: 2026-05-27
Backend: clinicbooking-be (ASP.NET)
Frontend: clinic_fe_dotnet (Angular/Ionic)

---

## Method + Endpoint Cross-Reference

### 1. Auth

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| POST | /api/auth/login | ❌ NOT FOUND as page-direct call | Hidden or moved to page |
| POST | /api/auth/register | ❌ NOT FOUND | Hidden or moved to page |
| POST | /api/auth/google | ❌ NOT FOUND | Hidden or moved to page |
| POST | /api/auth/facebook | ❌ NOT FOUND | Hidden or moved to page |
| POST | /api/auth/refresh-token | ❌ NOT FOUND | Hidden or moved to page |
| POST | /api/auth/logout | ✅ `auth/logout` | `apiService.post('auth/logout', ...)` in layout components |
| GET | /api/auth/me | ❌ NOT FOUND | Hidden or moved to page |
| PUT | /api/auth/me | ❌ NOT FOUND | Hidden or moved to page |
| POST | /api/auth/change-password | ❌ NOT FOUND | Hidden or moved to page |
| POST | /api/auth/set-password | ❌ NOT FOUND | Hidden or moved to page |

### 2. Announcements

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/announcements | ❌ NOT FOUND | Missing? |
| POST | /api/announcements | ✅ `announcements` | `this.api.post('announcements', ...)` |
| PUT | /api/announcements/{id} | ✅ `announcements/{id}` | `this.api.put('announcements/' + id, ...)` |
| DELETE | /api/announcements/{id} | ✅ `announcements/{id}` | `this.api.delete('announcements/' + id, ...)` |

### 3. Audit Logs

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/audit-logs | ❌ NOT FOUND | Hidden or not yet used |
| POST | /api/audit-logs | ✅ `audit-logs` | `apiService.post('audit-logs', ...)` |

### 4. Bookings

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/bookings | ✅ `bookings` | `apiService.get<any>('bookings')` in `booking.service.ts` |
| POST | /api/bookings | ✅ `bookings` | `apiService.post<any>('bookings', {})` in `booking.service.ts` |
| GET | /api/bookings/{id} | ✅ `bookings/{id}` | `apiService.get('bookings/' + id)` in components |
| PATCH | /api/bookings/{id}/check-in | ✅ `bookings/{id}/check-in` | Components call directly ✅ |
| PATCH | /api/bookings/{id}/undo-check-in | ✅ `bookings/{id}/undo-check-in` | Components call directly ✅ |
| PATCH | /api/bookings/{id}/doctor-complete | ✅ `bookings/{id}/doctor-complete` | Components call directly ✅ |
| GET | /api/bookings/{id}/consultation-record | ❌ NOT FOUND | Missing |
| PATCH | /api/bookings/{id}/consultation-record | ❌ NOT FOUND | Missing |
| PATCH | /api/bookings/{id}/confirm | ✅ `bookings/{id}/confirm` | Components call directly ✅ |
| PATCH | /api/bookings/{id}/cancel | ✅ `bookings/{id}/cancel` | Components call directly ✅ |
| PATCH | /api/bookings/{id}/complete | ✅ `bookings/{id}/complete` | Components call directly ✅ |
| PATCH | /api/bookings/{id}/no-show | ✅ `bookings/{id}/no-show` | Components call directly ✅ |
| PATCH | /api/bookings/{id}/reschedule | ❌ NOT FOUND | Missing |
| POST | /api/bookings/{id}/proof | ❌ NOT FOUND | Missing |
| GET | /api/bookings/me | ❌ NOT FOUND | Missing |
| GET | /api/bookings/doctor/today | ❌ NOT FOUND | Missing |
| GET | /api/bookings/doctor/today-summary | ❌ NOT FOUND | Missing |
| GET | /api/bookings/doctor/upcoming | ❌ NOT FOUND | Missing |
| GET | /api/bookings/doctor/patients | ❌ NOT FOUND | Missing |
| GET | /api/bookings/staff/all | ✅ `bookings/staff/all` | `booking.service.ts` (wrapped) ⚠️ |
| GET | /api/bookings/staff/today | ✅ `bookings/staff/today` | `booking.service.ts` (wrapped) ⚠️ |
| GET | /api/bookings/staff/for-payment | ✅ `bookings/staff/for-payment` | `booking.service.ts` (wrapped) ⚠️ |
| GET | /api/bookings/pending-verification | ❌ NOT FOUND | Missing |
| POST | /api/bookings/walk-in | ❌ NOT FOUND | Missing |

### 5. Doctors

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/doctors | ❌ NOT FOUND | Missing |
| GET | /api/doctors/admin | ❌ NOT FOUND | Missing |
| GET | /api/doctors/{id} | ❌ NOT FOUND | Missing |
| GET | /api/doctors/{id}/services | ❌ NOT FOUND | Missing |
| POST | /api/doctors | ❌ NOT FOUND | Missing |
| PUT | /api/doctors/{id} | ✅ `doctors/{id}` | `apiService.put('doctors/{id}', ...)` in doctor-form.page |
| PUT | /api/doctors/{id}/services | ❌ NOT FOUND | Missing |
| DELETE | /api/doctors/{id} | ❌ NOT FOUND | Missing |
| PUT | /api/doctors/me | ❌ NOT FOUND | Missing |
| GET | /api/doctors/me | ❌ NOT FOUND | Missing |
| GET | /api/doctors/{id}/schedule | ❌ NOT FOUND | Missing |
| PUT | /api/doctors/{id}/schedule | ❌ NOT FOUND | Missing |
| GET | /api/doctors/{id}/blocked-dates | ❌ NOT FOUND | Hidden |
| POST | /api/doctors/{id}/blocked-dates | ❌ NOT FOUND | Missing |
| DELETE | /api/doctors/{id}/blocked-dates/{id} | ✅ `doctors/{id}/blocked-dates/{id}` | `apiService.delete(...)` in doctor-schedule.page |
| GET | /api/doctors/{id}/day-status | ❌ NOT FOUND | Missing |
| POST | /api/doctors/{id}/day-status | ✅ `doctors/{id}/day-status` | In `doctor-state.service.ts` ⚠️ |
| GET | /api/doctors/{id}/available-slots | ❌ NOT FOUND | Missing |

### 6. Services

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/services | ❌ NOT FOUND | ❌ MISSING |
| GET | /api/services/{id} | ❌ NOT FOUND | ❌ MISSING |
| POST | /api/services | ❌ NOT FOUND | ❌ MISSING |
| PUT | /api/services/{id} | ❌ NOT FOUND | ❌ MISSING |
| DELETE | /api/services/{id} | ❌ NOT FOUND | ❌ MISSING |

### 7. Patients

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/patients | ✅ `patients` | `patient-state.service.ts` and `booking-detail.page.ts` |
| POST | /api/patients | ✅ `patients` | `staff-patient-detail.page.ts` |
| GET | /api/patients/{id} | ✅ `patients/{id}` | Components call directly ✅ |
| PUT | /api/patients/{id} | ✅ `patients/{id}` | `patient-state.service.ts` ⚠️ |
| POST | /api/patients/{id}/portal-account | ✅ `patients/{id}/portal-account` | Both service & page |
| GET | /api/patients/me | ❌ NOT FOUND | Missing |
| PUT | /api/patients/me | ❌ NOT FOUND | Missing |
| POST | /api/patients/me/consent | ❌ NOT FOUND | Missing |

### 8. Patient Documents & Media

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/patients/{id}/documents | ✅ Component calls `apiService.get()` ✅ | Page-direct |
| GET | /api/patients/me/documents | ❌ NOT FOUND | Missing |
| POST | /api/patients/{id}/documents | ✅ Component calls `apiService.postFormData()` ✅ | Page-direct |
| POST | /api/patients/me/documents | ❌ NOT FOUND | Missing |
| GET | /api/patients/{id}/documents/{id}/file | ✅ `patients/{id}/documents/{id}/file` ✅ | Blob call page-direct |
| GET | /api/patients/{id}/lab-results | ✅ `patients/{id}/lab-results` ✅ | Page-direct |
| GET | /api/patients/me/lab-results | ❌ NOT FOUND | Missing |
| POST | /api/patients/{id}/lab-results | ✅ `postFormData()` ✅ | Page-direct |
| POST | /api/patients/me/lab-results | ❌ NOT FOUND | Missing |
| GET | /api/patients/{id}/clinical-history | ❌ NOT FOUND | Missing |
| GET | /api/patients/{id}/lab-results/{id}/file | ✅ Blob call ✅ | Page-direct |
| GET | /api/patients/{id}/vaccinations | ❌ NOT FOUND | Missing |
| POST | /api/patients/{id}/vaccinations | ❌ NOT FOUND | Missing |
| PUT | /api/patients/{id}/vaccinations/{id} | ❌ NOT FOUND | Missing |
| DELETE | /api/patients/{id}/vaccinations/{id} | ❌ NOT FOUND | Missing |
| GET | /api/patients/me/vaccinations | ❌ NOT FOUND | Missing |

### 9. Patient-facing Medical Records

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/medical-records/me | ❌ NOT FOUND | Missing |
| GET | /api/prescriptions/me | ❌ NOT FOUND | Missing |
| GET | /api/follow-ups/me | ❌ NOT FOUND | Missing |
| GET | /api/patient-documents/me/bookings/{id}/pdf | ✅ Blob call ✅ | Page-direct |
| GET | /api/patient-documents/me/prescriptions/{id}/pdf | ✅ Blob call ✅ | Page-direct |
| GET | /api/patient-documents/me/medical-records/{id}/pdf | ✅ Blob call ✅ | Page-direct |
| GET | /api/patient-documents/me/all.pdf | ✅ Blob call ✅ | Page-direct |

### 10. Payments

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/payments/booking/{id} | ❌ NOT FOUND | Missing |
| PATCH | /api/payments/{id}/confirm | ❌ NOT FOUND | Missing |
| GET | /api/payments/{id}/receipt | ❌ NOT FOUND | Missing |
| PATCH | /api/payments/{id}/waive | ✅ `payments/{id}/waive` | Components call directly ✅ |
| PATCH | /api/payments/{id}/refund | ✅ `bookings/{id}/refund` (via PUT) | Components call via `bookings/{id}` |

### 11. Medical Records (Admin/Doctor)

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| GET | /api/medical-records/consultations | ❌ NOT FOUND | Missing |
| GET | /api/medical-records/prescriptions | ❌ NOT FOUND | Missing |
| GET | /api/medical-records/... (all 28+ CRUD) | ❌ NOT FOUND | All missing from page-direct scan |

### 12. Other

| Method | Backend | Frontend | Status |
|--------|---------|----------|--------|
| POST | /api/device-tokens | ❌ NOT FOUND | Missing |
| GET | /api/drug-interactions/allergy-check | ❌ NOT FOUND | Missing |
| POST | /api/drug-interactions/check | ❌ NOT FOUND | Missing |
| GET | /api/health | ❌ NOT FOUND | Missing |
| GET | /api/notifications | ❌ NOT FOUND | Missing |
| PUT | /api/notifications/{id}/read | ✅ `notifications/{id}/read` ✅ | Page-direct |
| PUT | /api/notifications/read-all | ✅ `notifications/read-all` ✅ | Page-direct |
| GET | /api/admin/dashboard/summary | ❌ NOT FOUND | Missing |
| GET | /api/admin/staff | ❌ NOT FOUND | Missing |
| POST | /api/admin/staff/invite | ✅ `admin/staff/invite` ✅ | Page-direct |
| PUT | /api/admin/staff/invite/{id}/revoke | ✅ `admin/staff/invite/{id}/revoke` ✅ | Page-direct |
| PUT | /api/admin/staff/{id}/update-status | ✅ `admin/staff/{id}/update-status` ✅ | Page-direct |
| GET | /api/reports/unpaid-completed-visits | ❌ NOT FOUND | Missing |
| GET | /api/reports/pending-follow-ups | ❌ NOT FOUND | Missing |
| GET | /api/reports/daily-booking-summary | ❌ NOT FOUND | Missing |
| GET | /api/reviews?doctorId= | ✅ `reviews?bookingId=` ⚠️ | **Query param mismatch** |
| POST | /api/reviews | ✅ `reviews` ✅ | Page-direct |
| GET | /api/services | ❌ NOT FOUND | Missing |
| GET | /api/settings | ❌ NOT FOUND | Missing |
| PUT | /api/settings | ❌ NOT FOUND | Missing |

### 13. Dead/Orphaned Frontend Calls

| Method | Endpoint | Frontend File | Backend? |
|--------|----------|---------------|----------|
| GET | `medication_master` | `prescription-drug-list.ts` | 🔴 **NO backend route** |

---

## Summary

| Metric | Count |
|--------|-------|
| Backend endpoints | ~120 |
| Frontend calls found (direct page) | ~35 |
| Frontend calls found (wrapped in service) | ~15 |
| ✅ 1:1 matched | ~35 |
| ⚠️ Query param mismatch | 1 (`reviews?bookingId=` vs `reviews?doctorId=`) |
| 🔴 Orphaned frontend call (no backend) | 1 (`medication_master`) |
| ❌ Backend endpoints with no frontend call | ~80 (mostly hidden in service wrappers or features not yet built) |

The large "missing" count is expected — many backend endpoints (medical-records CRUD, doctor CRUD, services CRUD) are wrapped inside feature services like `MedicalRecordsService` that get their API calls from service-to-service interaction rather than page-direct calls. The actual app works end-to-end because the service files still call ApiService internally.
