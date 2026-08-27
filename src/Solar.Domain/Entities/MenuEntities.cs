namespace Solar.Domain.Entities;

public class Resource
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public bool? Status { get; set; } = true;

    // Navigation
    public ICollection<Menu> Menus { get; set; } = [];
    public ICollection<PermissionResource> Permissions { get; set; } = [];
}

public class Menu
{
    public long Id { get; set; }
    public long? ResourceId { get; set; }
    public string? Name { get; set; }
    public bool? Status { get; set; } = true;
    public int Order { get; set; } = 999;
    public long? ParentId { get; set; }

    // Navigation
    public Resource? Resource { get; set; }
    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];
    public ICollection<MenuContext> MenuContexts { get; set; } = [];
}

public class Context
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Parameter { get; set; }

    // Navigation
    public ICollection<MenuContext> MenuContexts { get; set; } = [];
}

public class MenuContext
{
    public long MenuId { get; set; }
    public long ContextId { get; set; }

    // Navigation
    public Menu? Menu { get; set; }
    public Context? Context { get; set; }
}

public class PermissionResource
{
    public int ProfileId { get; set; }
    public long ResourceId { get; set; }
    public bool? PerId { get; set; } = false;
    public bool? Status { get; set; } = true;

    // Navigation
    public Profile? Profile { get; set; }
    public Resource? Resource { get; set; }
}
