using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mock;

public static class RouteMiddlewareExtensions 
{
  public static WebApplication UseExtraRoutes(this WebApplication app)
  {
    var writableDoc = JsonNode.Parse(File.ReadAllText("mock.json"));

    // print API
    foreach (var elem in writableDoc?.Root.AsObject().AsEnumerable()){
      Console.WriteLine(string.Format("GET /{0}", elem.Key.ToLower()));
      Console.WriteLine(string.Format("GET /{0}", elem.Key.ToLower()) + "/id");
      Console.WriteLine(string.Format("POST /{0}", elem.Key.ToLower()));
      Console.WriteLine(string.Format("DELETE /{0}", elem.Key.ToLower()) + "/id");
      Console.WriteLine(" ");
    }
 
    // setup routes
    foreach(var elem in writableDoc?.Root.AsObject().AsEnumerable()) {
      var arr = elem.Value.AsArray();
      app.MapGet(string.Format("/{0}", elem.Key), () => elem.Value.ToString());
      app.MapGet(string.Format("/{0}", elem.Key) + "/{id}", (int id) =>
      {
        var matchedItem = arr.SingleOrDefault(row => row
          .AsObject()
          .Any(o => o.Key.ToLower() == "id" && int.Parse(o.Value.ToString()) == id)
        );
        return matchedItem;
      });      
      app.MapPost(string.Format("/{0}", elem.Key), async (HttpRequest request) => {
        string content = string.Empty;
        using(StreamReader reader = new StreamReader(request.Body))
        {
          content = await reader.ReadToEndAsync();
        }
        var newNode = JsonNode.Parse(content);
        var array = elem.Value.AsArray();
        newNode.AsObject().Add("Id", array.Count() + 1);
        array.Add(newNode);
        
        File.WriteAllText("mock.json", writableDoc.ToString());
        return content;
      });
      app.MapPut(string.Format("/{0}", elem.Key), () => {
        return "TODO";
      });
      app.MapDelete(string.Format("/{0}", elem.Key) + "/{id}", (int id) => {

        var matchedItem = arr
         .Select((value, index) => new{ value, index})
         .SingleOrDefault(row => row.value
          .AsObject()
          .Any(o => o.Key.ToLower() == "id" && int.Parse(o.Value.ToString()) == id)
        );
        if (matchedItem != null) {
          arr.RemoveAt(matchedItem.index);
          File.WriteAllText("mock.json", writableDoc.ToString());
        }
        
        return "OK";
      });

    };
    
    return app;
  }
}