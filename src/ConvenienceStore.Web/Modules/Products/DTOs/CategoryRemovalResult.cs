namespace ConvenienceStore.Web.Modules.Products.DTOs;

public enum CategoryRemovalStatus { Success, Invalid, NotFound, Conflict, Referenced }

public sealed record CategoryRemovalResult(CategoryRemovalStatus Status);
