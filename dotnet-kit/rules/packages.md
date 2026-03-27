# Rule: NuGet Package Management

## DO
- Use a **`Directory.Packages.props`** file at the solution root for centralized version management
- Pin **exact versions** in `Directory.Packages.props` — no floating versions in production
- Use `<PackageReference>` with `Version=""` in `.csproj` (version comes from central props)
- Keep `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>` in every project
- Run `dotnet list package --outdated` and `dotnet list package --vulnerable` regularly
- Use `dotnet add package <name>` — let NuGet resolve to `Directory.Packages.props`
- Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`
- Review transitive dependencies before upgrading major versions

## DON'T
- Don't use floating versions (`*`, `>= 1.0`) in production projects
- Don't add packages to multiple `.csproj` files with different versions
- Don't reference packages not needed by a given layer (e.g., EF Core in Domain)
- Don't leave unused package references — they increase restore time and attack surface
- Don't use `--prerelease` packages in production without explicit pinning and justification
- Don't ignore `dotnet list package --vulnerable` output

## Directory.Packages.props Template
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Core -->
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="8.0.0" />
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="Testcontainers.MsSql" Version="4.1.0" />
  </ItemGroup>
</Project>
```

## Directory.Build.props Template
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```
