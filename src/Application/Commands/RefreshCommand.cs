using MediatR;
using Application.DTOs;

namespace Application.Commands;

public record RefreshCommand(RefreshRequest Request) : IRequest<AuthResponse>;
