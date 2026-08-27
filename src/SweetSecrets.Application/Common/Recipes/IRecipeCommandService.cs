namespace SweetSecrets.Application.Common.Recipes;

public interface IRecipeCommandService
{
    Task<CreateRecipeResult> CreateAsync(
        CreateRecipeCommand command,
        CancellationToken cancellationToken = default);

    Task<AddRecipeItemResult> AddItemAsync(
        AddRecipeItemCommand command,
        CancellationToken cancellationToken = default);

    Task<UpdateRecipeResult?> UpdateAsync(
        UpdateRecipeCommand command,
        CancellationToken cancellationToken = default);

    Task<UpdateRecipeItemResult?> UpdateItemAsync(
        UpdateRecipeItemCommand command,
        CancellationToken cancellationToken = default);

    Task<RemoveRecipeItemResult?> RemoveItemAsync(
        RemoveRecipeItemCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
        Guid recipeId,
        bool isActive,
        CancellationToken cancellationToken = default);
}