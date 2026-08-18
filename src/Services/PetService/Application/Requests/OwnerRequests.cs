namespace PetService.Application.Requests;

public record CreateOwnerRequest(
    string OwnerName,
    string Email,
    string Phone,
    string? Address);

public record UpdateOwnerRequest(
    string OwnerName,
    string Email,
    string Phone,
    string? Address);
