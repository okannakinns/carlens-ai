using Microsoft.AspNetCore.Mvc;

namespace Carlens.Web.Security;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AnalysisRateLimitAttribute()
    : TypeFilterAttribute(typeof(AnalysisRateLimitFilter));
