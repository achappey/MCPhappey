
namespace MCPhappey.SQL.WebApi.Extensions;

public static class DtoExtensions
{
    public static Models.Dto.PromptArgument ToDto(this Models.Database.PromptArgument db)
      => new()
      {
          Name = db.Name,
          Description = db.Description,
          Required = db.Required
      };

    // Prompt: Dto → Database
    public static Models.Database.PromptArgument ToDb(this Models.Dto.PromptArgument dto)
        => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            Required = dto.Required
        };

    public static Models.Dto.Prompt ToDto(this Models.Database.Prompt db)
        => new()
        {
            Name = db.Name,
            Description = db.Description,
            PromptTemplate = db.PromptTemplate,
            Arguments = db.Arguments?.Select(a => a.ToDto()).ToList() ?? []
        };

    // Prompt: Dto → Database
    public static Models.Database.Prompt ToDb(this Models.Dto.Prompt dto)
        => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            PromptTemplate = dto.PromptTemplate,
            Arguments = dto.Arguments?.Select(a => a.ToDb()).ToList() ?? []
        };

    // Resource: Database → Dto
    public static Models.Dto.Resource ToDto(this Models.Database.Resource db)
        => new()
        {
            Uri = db.Uri,
            Name = db.Name,
            Description = db.Description,
        };

    // Resource: Dto → Database
    public static Models.Database.Resource ToDb(this Models.Dto.Resource dto)
        => new()
        {
            Uri = dto.Uri,
            Name = dto.Name,
            Description = dto.Description
        };

    public static IEnumerable<Models.Dto.Prompt> ToDto(this IEnumerable<Models.Database.Prompt> dbs)
        => dbs.Select(p => p.ToDto());

    public static IEnumerable<Models.Database.Prompt> ToDb(this IEnumerable<Models.Dto.Prompt> dtos)
        => dtos.Select(p => p.ToDb());

    // Resource: Database → Dto
    public static Models.Dto.ResourceTemplate ToDto(this Models.Database.ResourceTemplate db)
        => new()
        {
            UriTemplate = db.TemplateUri,
            Name = db.Name,
            Description = db.Description,
        };

    // Resource: Dto → Database
    public static Models.Database.ResourceTemplate ToDb(this Models.Dto.ResourceTemplate dto)
        => new()
        {
            TemplateUri = dto.UriTemplate,
            Name = dto.Name,
            Description = dto.Description
        };

    public static Models.Database.Server ToDb(this Models.Dto.Server dto)
   => new()
   {
       Name = dto.Name,
       Instructions = dto.Instructions
   };
}