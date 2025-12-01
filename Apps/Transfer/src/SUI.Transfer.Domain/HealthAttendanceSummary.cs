namespace SUI.Transfer.Domain;

public record HealthAttendanceSummary(
    int MissdGPAppointments,
    int MissedHospitalAppointments,
    int EmergencyDepartmentAttendances
);
