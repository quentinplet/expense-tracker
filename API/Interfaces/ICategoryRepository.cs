using System;
using API.Entities;

namespace API.Interfaces;

public interface ICategoryRepository
{
    /// Catégories de l'utilisateur, triées par Type puis Name. `type` filtre en
    /// plus si fourni (sélecteurs de formulaire).
    Task<List<Category>> GetAllAsync(Guid userId, TransactionType? type = null);
    Task<Category?> GetByIdAsync(Guid id);

    /// Cible de réaffectation à la suppression d'une catégorie utilisée — la
    /// catégorie verrouillée ("Other") du même Type pour cet utilisateur.
    Task<Category?> GetLockedByTypeAsync(Guid userId, TransactionType type);

    /// Unicité par (UserId, Name, Type) — exclut `excludeId` sur un update.
    Task<bool> ExistsAsync(Guid userId, string name, TransactionType type, Guid? excludeId = null);

    void Add(Category category);
    void Update(Category category);
    void Delete(Category category);

}
