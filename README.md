# mock-sharp
Mock API for minimal API, ASP .NET, written in C#

## Run

Type `dotnet run`

```bash
dotnet run
```

This should show the supported routes in the terminal

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
