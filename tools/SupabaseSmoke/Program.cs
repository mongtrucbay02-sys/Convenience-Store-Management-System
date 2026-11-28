using SmartMart.SupabaseSmoke;

// No ASP.NET host, seeders or migrations are started by this executable.
return await SmokeRunner.RunAsync(
    Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION"),
    args.FirstOrDefault() ?? "artifacts/supabase-smoke/result.json",
    Console.Out, Console.Error);
