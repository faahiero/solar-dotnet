namespace Solar.Domain.Enums;

/// <summary>
/// Tipo de questão no banco de questões do Solar.
/// Mapeado a partir de Question::UNIQUE, Question::MULTIPLE, etc. em app/models/question.rb.
/// </summary>
public enum QuestionType
{
    SingleChoice = 0,   // Escolha única (apenas 1 item correto)
    Multiple = 1,       // Múltipla escolha clássica com desconto
    TrueFalse = 2,      // Verdadeiro ou Falso (cada item é V ou F)
    MultipleWeighted = 3 // Múltipla escolha ponderada (Multiple New)
}
