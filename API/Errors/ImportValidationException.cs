namespace API.Errors;

/// Erreur de validation métier détectée pendant le parsing ou la confirmation d'un
/// import CSV (fichier illisible, catégorie invalide...) — ImportController la
/// traduit en 400, jamais laissée remonter à ExceptionMiddleware (qui répondrait 500).
public class ImportValidationException(string message) : Exception(message);
