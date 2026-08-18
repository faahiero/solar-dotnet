namespace Solar.Domain.Enums;

/// <summary>
/// Bitmask de tipos de perfil do sistema Solar.
/// Mapeado a partir das constantes em config/environment.rb e queries bitwise do Rails.
/// </summary>
[Flags]
public enum ProfileType
{
    NoType = 0,
    Basic = 1 << 0,            // 1 (0b00000001) - Usuário Básico
    ClassResponsible = 1 << 1, // 2 (0b00000010) - Professor / Responsável
    Student = 1 << 2,          // 4 (0b00000100) - Aluno
    Editor = 1 << 3,           // 8 (0b00001000) - Editor de Conteúdo
    Admin = 1 << 4,            // 16 (0b00010000) - Administrador Global
    Observer = 1 << 5,         // 32 (0b00100000) - Observador / Tutor
    Coordinator = 1 << 6       // 64 (0b01000000) - Coordenador de Curso
}
