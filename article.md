> TLDR; this article describes how to create a Mock API from a JSON file for minimal API in ASP .NET

## What and why Mock APIs

To mock something means you respond with some kind of fake data, that data can be in-memory, from a file or soome kind of tool generating a bunch of endpoints. There are some reasons why mocking an API could be a good idea:

- **Different teams work at different speeds**. Lets say your app is built by two different teams, or developers and one is faster than the other. Typically that's the frontend team being the faster one (not always) cause there's a lot to wire up in the backend.
- **You start with the frontend first**. Your team/developer have maybe decided to build a full vertical and starts with the frontend and slowly work their way towards the backend and the data source.

Ok, so we established there might be a need to mock your API. So how do we do it? You want to be able to specify the data you want to mock and there are some formats out there that makes sense to have such mocked data in like JSON, XML or CSV perhaps. For the sake of this article, we will go with JSON

## Planning our project, what we need to do

Ok, so high-level, we need to do the following:
- **Create a file in JSON**, containing our routes and the response. We imagine the JSON file looking something like so:

   ```json
   {
      "Products": [
        {
          "Id": 1,
          "Name": "Mock"
        },
        {
          "Id": 2,
          "Name": "Second Mock"
        }
      ]
   }
   ```

- **What routes do we want?** A good API should implement GET, POST, PUT and DELETE to support a RESTful approach.
- **Responding to changes.** So what should happen if the user actually calls POST, PUT or DELETE? Reasonably, the mocked file should change.

Ok, so we know high-level what we need, and how things should behave, lets see if we can choose our technical approach next.

## Approach - let's create the solution

The normal way to setup routes, in Minimal API, is to call code like so:

```csharp
app.MapGet("/", () => "Hello World!");
```

By calling `MapGet()` we create a route to "/" that when called responds with "Hello World". For the sake of our API, we will have to call `MapGet()`, `MapPost()`, `MapPut()` and `MapDelete()`. 

> Here be dragons. Many of you, I'm sure, are used to working with JSON in a typed manor, meaning you are likely to create types for your classes and rely on methods like `Deserialize()` and `Serialize()`. That's a great approach, however, for a mocked API that doesn't even exist yet, this code doesn't rely on any of that :)

### Defining the routes, making it loosely coupled

It would be neat if these routes were loosely coupled code that we could just bring in, when developing, and removed when we are live with our app.

When `app.MapGet()` was called, it invoked an instance of the class `WebApplication`. By creating an extension method on said class, we have a way an approach to add code in a way that it's nicely separated.  We also need a static class to put said extension method in. That means our code starting out should look something like so:

```csharp
public static class RouteMiddlewareExtensions 
{
  public static WebApplication UseExtraRoutes(this WebApplication app)
  {
  }
}
```

## Exercise - Read from a mock file, and add support for `GET`

Ok, we know how we are starting, a static class and an extension method, so lets make that happen:

1. Run `command`, to generate a new minimal API project

   ```bash
   TODO
   cd
   ```

1. Create a file *MockMiddleware.cs* and give it the following code:

   ```csharp
   using System.Text.Json;
   using System.Text.Json.Nodes;

   public static class RouteMiddlewareExtensions
   {
      public static WebApplication UseExtraRoutes(this WebApplication app)
      { 
      }
   }
   ```

1. Add code to read a JSON file into a JSON representation:

   ```csharp
   var writableDoc = JsonNode.Parse(File.ReadAllText("mock.json"));
   ```

   Note the choice of JsonNode, this is so we can make the JSON doc writable, which we will need for POST, PUT and DELETE laterr on.

1. Create the file *mock.json* and give it the following content:

   ```json
   {
      "Products": [
        {
          "Id": 1,
          "Name": "Mock"
        },
        {
          "Id": 2,
          "Name": "Second Mock"
        }
      ],
      "Orders": [
        {
          "Id": 1,
          "Name": "Order1"
        },
        {
          "Id": 2,
          "Name": "Second Order"
        }
      ]
    }
   ```

### Add `GET`

Lets support our first HTTP verb, GET. 

1. Add the following code:

   ```csharp
   foreach(var elem in writableDoc?.Root.AsObject().AsEnumerable()) {
      var arr = elem.Value.AsArray();
      app.MapGet(string.Format("/{0}", elem.Key), () => elem.Value.ToString());
   }
   ```

   In the above code, we navigate into the root object. Then, we convert it to an object representation and starts iterating over the keys, according to the mock file, that means `Products` and `Orders`. Lastly, we setup the route and the callback, the route is at `elem.Key` and the value we want to return is at `elem.Value`.

1. In the file *Program.cs* add the following line:

   ```csharp
   app.UseExtraRoutes();
   ```

   The preceding code will ensure our routes are added to the app.

1. Run `dotnet run`, to run the app

   ```bash
   dotnet run
   ```

1. Navigate to the port indicated in the console outputt and navigate to `/products` and `/orders`, they should both show an output

### Add `GET` by id

Ok, you got the basic GET case to work, what about filtering the data with parameter. Using `/products/1`, should just return one record back. How do we do that?

1. Add the following code in the foreach loop in *MockMiddlware.cs*:

   ```csharp
   app.MapGet(string.Format("/{0}", elem.Key) + "/{id}", (int id) =>
      {
        var matchedItem = arr.SingleOrDefault(row => row
          .AsObject()
          .Any(o => o.Key.ToLower() == "id" && int.Parse(o.Value.ToString()) == id)
        );
        return matchedItem;
      });
   ```

   The above code is iterating over the rows for a specific route and looks for an `id` property that matches our `{id}` pattern. The found item is returned back.

1. Run `dotnet run` to test out this code:

   ```bash
   dotnet run
   ```

1. Navigate to `/products/1`, you should see the following JSON output:

   ```json
   {

      "Id": 1,
      "Name": "Mock"
   }
   ```

   Great, we got it to work.

## Exercise - write data

Now that we can read data from our mock api, lets tackle writing data. The fact that we were `JsonNode.Parse()` in the beginning makes it possible for use to use operations on the `JsonNode` instance. In short, our approach will be:

- find the specific place in the `JsonNode`, that represents our mock data, and change it
- save down the whole `JsonNode` instance to our *mock.json*. If the user uses an operation to change the data, that should be reflected in the Mock file.

### Add `POST`

To implement this route, we will use `MapPost()` but we can't just give it a typed object in the callback for the route, because we don't know what it looks like. Instead, we will use the request object, read the body and add that to the `JsonNode`.

1. Add following code to support `POST`:

   ```csharp
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
   ```

   In the above code, we have `request` as input parameter to our route handler function. 

   ```csharp
   app.MapPost(string.Format("/{0}", elem.Key), async (HttpRequest request) => {});
   ```

   Then we read the body, using a `StreamReader`.  

   ```csharp
   using(StreamReader reader = new StreamReader(request.Body))
   {
     content = await reader.ReadToEndAsync();
   }
   ```

   Next, we construct a JSON representation from our received BODY:

   ```csharp
   var newNode = JsonNode.Parse(content);
   ```

   This is followed by locating the place to insert this new JSON and adding it:

   ```csharp
   var array = elem.Value.AsArray();
   newNode.AsObject().Add("Id", array.Count() + 1);
   array.Add(newNode);
   ```

   Lastly, we update the mock file and respond something back to the calling client:

   ```csharp
   File.WriteAllText("mock.json", writableDoc.ToString());
   return content;
   ```

### Add `DELETE`

To support deletion, we need a very similar approach to how we located an entry by id parameter. We also need to locate where to delete in the `JsonObject`.

1. Add the following code to support delete:

   ```csharp
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
   ```

   First, we find the item in question, but we also make sure that we know what the index of the found item is. We will use this index later to remove the item. Hence, we get the following code:

   ```csharp
   var matchedItem = arr
         .Select((value, index) => new{ value, index})
         .SingleOrDefault(row => row.value
          .AsObject()
          .Any(o => o.Key.ToLower() == "id" && int.Parse(o.Value.ToString()) == id)
        );
   ```

   Our `matchedItem` now contains either NULL or an object that has an `index` property. Using this `index` property, we will be able to perform deletions:

   ```csharp
   if (matchedItem != null) {
     arr.RemoveAt(matchedItem.index);
     File.WriteAllText("mock.json", writableDoc.ToString());
   }
   ```

To test writes, use something like Postman or Advanced REST client, it should work.

### Add route info

We're almost done, as courtesy towards the programmer using this code, we want to print out what routes we have and support so it's easy to know what we support. 

1. Add this code, just at the start of the method `UseExtraRoutes()`:

   ```csharp
    // print API
    foreach (var elem in writableDoc?.Root.AsObject().AsEnumerable()){
      Console.WriteLine(string.Format("GET /{0}", elem.Key.ToLower()));
      Console.WriteLine(string.Format("GET /{0}", elem.Key.ToLower()) + "/id");
      Console.WriteLine(string.Format("POST /{0}", elem.Key.ToLower()));
      Console.WriteLine(string.Format("DELETE /{0}", elem.Key.ToLower()) + "/id");
      Console.WriteLine(" ");
    }
   ```

That's it, that's all we intend to implement. Hopefully this is all useful to you and you will be able to use it next you just want an API up and running that you can build a front end app off of.  

### Full code

If you got lost at any point, here's the full code:

*Program.cs*

```csharp
using Mock;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.UseExtraRoutes(); // this is where our routes gets added

app.Run();
```

*MockMiddleware.cs*

```csharp
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
```

### Update - homework

For your homework, see if you can implement PUT. :)

## Summary

I took you through a journey of implementing a Mock API for minimal APIs. Hopefully you found this useful and is able to use it in a future project.
