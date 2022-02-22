# mock-sharp

Mock API for minimal API, ASP .NET, written in C#

## Run

Type `dotnet run`

```bash
dotnet run
```

This should show the supported routes in the terminal

## NuGet package

Coming...

## Features

Given a mock file, (for now it's hardcoded as *mock.json*), with the following JSON content:

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

it will create the below routes:

|Verb  |Route  | Description |
|---------|---------|---------|
|GET     |  /products       | fetches a list of products        |
|GET     | /products/{id} | fetches one product, given unique identifier `id` |
|POST    | /products | creates a new product, assumes a JSON represenation is sent via the BODY |
|DELETE | /products/{id} | deletes one product, given unique identifier `id` |
|PUT | | Coming.. |

### Query parameters

Coming

## Mock data

mock data is in *mock.json*. Here's an example of what it could look like:

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
