using System.Text.Json;
using System.Net.Http.Headers;

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
        // 1. Base64文字列をバイト配列(画像データ)に変換
        var base64Data = request.ImageBase64.Contains(",") ? request.ImageBase64.Split(',')[1] : request.ImageBase64;
        var imageBytes = Convert.FromBase64String(base64Data);

        // 2. SauceNAO APIに送信するフォームデータを作成
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(apiKey), "api_key");
        content.Add(new StringContent("2"), "output_type"); // 2 = JSON形式を要求
        
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "file", "image.jpg");

        // 3. SauceNAOへリクエスト送信
        var response = await httpClient.PostAsync("https://saucenao.com/search.php", content);
        
        // ★重要: いきなりJSONとして解釈せず、まずは「ただの文字列」として読み取る
        var responseString = await response.Content.ReadAsStringAsync();
        Console.WriteLine("\n=== SauceNAOからの応答 ===");
        Console.WriteLine(responseString);
        Console.WriteLine("==========================\n");

        // 4. 返ってきた文字列がJSONの開始文字 "{" で始まっているかチェック
        if (!responseString.TrimStart().StartsWith("{"))
        {
            // SauceNAOが "error..." などの文字列を返してきた場合の安全処理
            return Results.Ok(new SearchResponse { Character = "検索失敗", Title = "APIの制限またはエラー" });
        }

        // 5. 安全が確認できたらJSONとして解析
        var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;

        string character = "不明";
        string title = "不明";

        // 結果が含まれているか階層を安全にチェック
        if (root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
        {
            var firstResult = results[0].GetProperty("data");
            
            // キャラクター名の取得
            if (firstResult.TryGetProperty("characters", out var charaProp))
            {
                character = charaProp.GetString() ?? "不明";
            }
            // 作品名の取得（SauceNAOは title または source に入っていることが多い）
            if (firstResult.TryGetProperty("title", out var titleProp) || 
                firstResult.TryGetProperty("source", out titleProp))
            {
                title = titleProp.GetString() ?? "不明";
            }
        }
        else
        {
            return Results.Ok(new SearchResponse { Character = "特定できませんでした", Title = "検索結果なし" });
        }

        return Results.Ok(new SearchResponse { Character = character, Title = title });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[システムエラー]: {ex.Message}");
        return Results.Ok(new SearchResponse { Character = "システムエラー", Title = "エラー" });
    }
});

app.Run();

// データの受け渡し用クラス
public class SearchRequest { public string ImageBase64 { get; set; } = ""; }
public class SearchResponse { public string Character { get; set; } = ""; public string Title { get; set; } = ""; }