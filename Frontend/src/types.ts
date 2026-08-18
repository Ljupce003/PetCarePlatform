export interface Owner { ownerId: string; ownerName: string; email: string; phone: string; address?: string | null }
export interface Pet { petId: string; ownerId: string; name: string; species: string | number; breed?: string | null; birthDate: string; weight: number; microchipNumber?: string | null; allergies: string[]; chronicConditions: string[] }
export interface Clinic { clinicId: string; name: string; location: string; address: string }
export interface Veterinarian { veterinarianId: string; clinicId: string; fullName: string; specialization: string; isAvailable: boolean }
export interface Slot { availabilitySlotId: string; veterinarianId: string; veterinarianName: string; specialization: string; clinicId: string; clinicName: string; startsAtUtc: string; endsAtUtc: string }
export interface Appointment { appointmentId: string; petId: string; ownerId: string; clinicId: string; veterinarianId: string; availabilitySlotId: string; startsAtUtc: string; endsAtUtc: string; reason: string; status: string; cancellationReason?: string | null; createdAtUtc: string }
export interface Examination { id: string; petId: string; ownerId: string; veterinarianId: string; appointmentId?: string | null; examinedAtUtc: string; diagnosis: string; treatmentPlan: string; medications: string[]; nextControlAtUtc?: string | null; notes?: string | null }
export interface Vaccination { id: string; petId: string; ownerId: string; veterinarianId: string; vaccineName: string; administeredOn: string; nextDueOn?: string | null; batchNumber?: string | null }
export interface Notification { id: string; ownerId: string; petId: string; type: string | number; title: string; message: string; scheduledForUtc: string; status: string | number; createdAtUtc: string; sentAtUtc?: string | null }
export interface Session { accessToken: string; subject: string; username: string; roles: string[]; expiresAt: number }
