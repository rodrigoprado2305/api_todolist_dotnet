# API ToDo List — .NET 9 (C#) + MySQL

Ele é uma aplicação simples de gerenciamento de tarefas (TODO) usando o .net para criar uma API RESTful e o MySQL como banco de dados.

## Requisitos

- .NET SDK 9
- MySQL 5.7 (ou compatível)

## Extesion: MarketPlace, instalar...
Code Runner
c# Dev kit
c# Base language support for c#
IntelliCode for c# Dev Kit

## Commands, scan
dotnet --list-sdks
findstr ms-dotnettools.csharp 

## Database

MySQL 5.7.44.0 - Usando docker
```
docker run --name mysql5.7 -d -p 3306:3306 -e MYSQL_ROOT_PASSWORD=Banco12345* mysql:5.7
```

CREATE DATABASE IF NOT EXISTS todo_db
USE todo_db;

CREATE TABLE IF NOT EXISTS tasks (
  id INT AUTO_INCREMENT PRIMARY KEY,
  title VARCHAR(200) NOT NULL,
  description TEXT NULL,
  completed TINYINT(1) NOT NULL DEFAULT 0
);

## Run

No diretório `src/Api`:
```bash
dotnet restore
dotnet build
dotnet run --project src/Api/Api.csproj --urls http://localhost:5035
```
A saída mostrará a porta (ex.: `http://localhost:5035` / `https://localhost:7201`).

Para Release: 
dotnet run -c Release --urls http://localhost:5035

Para usar um profile do launchSettings.json:
dotnet run --launch-profile "API Todo (.NET 9) – Debug Program.cs"


# Swagger

http://localhost:5035/swagger

## Rotas
- `POST /login` → retorna `{ "token": "..." }` (JWT HS256).  
  
http://localhost:5035/login
body: {
  "username": "rodrigo",
  "password": "vini123"
}

  - `POST /tasks`
  http://localhost:5035/tasks
{
  "title": "Minha nova tarefa - dotnet 01",
  "description": "Descrição detalhada da tarefa - dotnet 01",
  "completed": false
}

  - `GET /tasks`
  - `GET /tasks/:id`
  - `PUT /tasks/:id`
  - `DELETE /tasks/:id`

## Methods - Example - POSTMAN

Type|Rote|Description
-|-|-
POST|http://localhost:5035/tasks|Insert taks
GET|http://localhost:5035/tasks/3|Search one task
GET|http://localhost:5035/tasks|List all task
DELETE|http://localhost:5035/tasks/1|Delete task
PUT|http://localhost:5035/tasks/3|Update task  
