# Web Api Circuit Breaker

This is a configurable circuit breaker class that can be used in Web API solutions. It is based on the <a href="https://en.wikipedia.org/wiki/Circuit_breaker_design_pattern">circuit breaker pattern</a>. 

The circuit breaker allows you to do the following:

* Define multiple rules that open a circuit.
* Open the circuit per API per-route or per-client.
* Configure the response during an open circuit and the time period it will stay open.
* Configure white-listed and black-listed clients.
* Configure per-server behavior.
* Load configuration from external sources.
* Define custom IP address parsers for proxy or non-IIS hosting scenarios.
