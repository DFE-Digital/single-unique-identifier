namespace SUI.Transfer.Domain;

public record HealthAttendanceSummary(
    int CountOfMissedGPAppointments,
    int CountOfMissedHospitalAppointments,
    int CountOfMissedCommunityHealthAppointments,
    int CountOfEmergencyDepartmentAttendances
);
