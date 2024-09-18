# API-Product-NET8

# Product API

A **Product API** é um serviço completo para gerenciamento de produtos, desenvolvido com arquitetura de microsserviços. Ele permite realizar operações CRUD (Criar, Ler, Atualizar e Deletar) em um banco de dados **MongoDB**. A API também utiliza **Redis** para implementar **cache** nas consultas, otimizando o desempenho nas leituras, e é conectado com AWS SQS (simulada com **LocalStack**) para enviar mensagens assíncronas, permitindo comunicação eficiente com outros serviços/microsserviços.


## Tecnologias Utilizadas

- .NET 8
- MongoDB
- Redis
- AWS SQS (simulando com **LocalStack**)
- Docker
- DDD (Domain-Driven Design)
- Microsserviços
- Logs
- Código limpo e documentado
- Swagger

## Como Rodar

### Pré-requisitos

Certifique-se de ter o Docker e o LocalStack instalados em seu ambiente.

1. **Subir o MongoDB e Redis com Docker:**

    ```bash
    docker run -d -p 27017:27017 --name mongodb mongo:latest
    docker run -d -p 6379:6379 --name redis redis:latest

2. **Executar o LocalStack para simular a fila SQS:**

   ```bash
   baixe e instale: https://app.localstack.cloud/getting-started
   abra o CMD e rode: localstack start
   Entre no site: https://app.localstack.cloud/sign-in
   E selecione o SQS na aba status.

3. **Criar a fila SQS no LocalStack:**
   
    ```bash
    aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name Product

4. **Criar um novo produto**
   Depois de subir a aplicação, use o swagger para criar/adicionar com os metodos CRUD, quando adicionado um novo produto a api envia para a fila SQS

## Endpoints Disponíveis
- POST **/api/Product:** Cria um novo produto.
- GET **/api/Product/{id}:** Busca um produto, primeiro no cache, se não tiver busca no mongoDB.
- PUT **/api/Product/{id}:** Atualiza um produto existente.
- DELETE **/api/Product/{id}:** Deleta um produto.

## Rodar a API
Após configurar o MongoDB, Redis e LocalStack, siga os passos abaixo para rodar a API:

1. **Clone o repositório.**
   
     ```bash
      https://github.com/CrJunior08/API-Product-NET8.git
   
2. **Execute a API**
   
   ```bash
     Aperte no play com o nome http da aplicação e execute
   
A API estará acessível com a documentação Swagger em http://localhost:5031/swagger.

## Integração com Redis

As consultas de produto são armazenadas em cache usando Redis. O cache é limpo após operações de atualização ou deleção.

## Integração com AWS SQS

Quando um novo produto é criado, uma mensagem é enviada para a fila SQS simulada pelo LocalStack.

## Logs

A aplicação implementa logging detalhado para todas as operações, incluindo logs de erros e mensagens informativas. Isso facilita a monitoração e depuração da API.

## Microsserviços

Esta API é construída como parte de uma arquitetura de microsserviços, onde ela se comunica com outro serviço (worker: https://github.com/CrJunior08/sqs-consumer-worker.git) que consome mensagens da fila SQS.
