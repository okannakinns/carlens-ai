using System;
using System.Collections.Generic;
using System.Text;

namespace Carlens.Domain.Common;

public abstract class BaseEntity
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public void Delete () 
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
    }
}
