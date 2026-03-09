# Architecture Overview

## Goals
- Scalability
- Low coupling
- Data-driven workflows
- Clear ownership of responsibilities

## High-Level Layers

Core  
Systems  
Data  
Presentation  
Services  

## Communication

- Interfaces
- Event bus
- Dependency injection (manual)

## Key Patterns

- State pattern (AI)
- Strategy pattern (weapons, abilities)
- Observer pattern (events)
- Object pooling
- Data-driven configuration

## Update Strategy

- Minimal MonoBehaviour logic
- System-driven update loops
- Explicit ownership of responsibilities

## Performance Philosophy

- Pool before instantiate
- Cache before query
- Measure before optimize
