This is a simple Ping Pong micro-service in C# .NET
- The app service handles the main logic: It stores ping requests, calls the ping_listener service, and updates the records with the pong response.
- The ping_listener service is a simple responder: It only responds to ping requests with a pong.
- CosmosDB stores the interactions: Both the ping request and the pong response are stored in CosmosDB for later retrieval.

- The services are orchestrated using Docker Compose. The docker-compose.yml file defines the three services (app, ping_listener, and cosmosdb-emulator), their ports, and how they are networked together.
- Docker Compose sets up a bridge network (mynetwork) that allows the services to communicate with each other by their service names. For example, the app service can reach the ping_listener service by sending requests to http://ping_listener.


To start this project:
Docker-compose up --build

- The server will then run on a port on local host.

- You can open up the Azure Cosmos DB Emulator to see if the DB is up and running at : https://localhost:8081/_explorer/index.html

Once the DB is running :
- You can open a new cmd line and do a POST request followig the cmd:
curl -X POST http://localhost:5000/ping

You should then receive "pong" message back with an Id and timestamp attached.

You can then follow in the Docker logs the calls made to the 'ping_listener' service and the updates of the records with the 'pong' response.

You can then go back to the page https://localhost:8081/_explorer/index.html and see that the DB has been created in "Explorer" and see the data in the item. 

To run the tests : 
Simply move to service/tests and run the cmd: dotnet test or dotnet test -l "console;detailed" (to see the actual output)
The tests will then run in your terminal and the output will be shown once the tests runned.

And you're all done.
