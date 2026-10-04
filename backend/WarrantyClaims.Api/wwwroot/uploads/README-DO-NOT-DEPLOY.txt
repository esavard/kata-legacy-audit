This folder is served directly by UseStaticFiles() with no authentication - see
Startup.cs. It's like that in the real (fictional) Azure deployment too. Do not "fix"
this by adding authorization middleware in front of wwwroot unless that's the specific
exercise you're doing.
