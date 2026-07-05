using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
var app = builder.Build();
app.UseCors();

var httpClient = new HttpClient();
var apiKey = "YourAPIKey";

app.MapPost("/api/search", async (SearchRequest request) =>
{
    try
    {
        var base64Part = request.ImageBase64.Contains(",") ? request.ImageBase64.Split(',')[1] : request.ImageBase64;
        var imageBytes = Convert.FromBase64String(base64Part);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(imageBytes), "file", "image.jpg");
        content.Add(new StringContent(apiKey), "api_key");
        content.Add(new StringContent("2"), "output_type");

        var response = await httpClient.PostAsync("https://saucenao.com/search.php", content);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // 「results」配列が存在するか確認
        if (json.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
        {
            var topResult = results[0];
            if (topResult.TryGetProperty("data", out var data))
            {
                // ここで「source」や「characters」を安全に取得
                string title = data.TryGetProperty("source", out var s) ? s.ToString() : "作品名不明";
                string chara = data.TryGetProperty("characters", out var c) ? c.ToString() : "キャラ名不明";
                
                // もし両方不明なら、他の候補を探すか、最低限の情報を返す
                return Results.Ok(new SearchResponse { Character = chara, Title = title });
            }
        }
        
        return Results.Ok(new SearchResponse { Character = "特定できませんでした", Title = "検索結果なし" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"エラー内容: {ex.Message}");
        return Results.Ok(new SearchResponse { Character = "検索エラー", Title = "API通信に失敗" });
    }
});

app.Run();

public class SearchRequest { public string ImageBase64 { get; set; } = ""; }
public class SearchResponse { public string Character { get; set; } = ""; public string Title { get; set; } = ""; }