
###TeaTime API v0.7.3b


**Timed Callbacks**
- .Add(
- .Loop(

**Queue Modes**
- .Wait()
- .Repeat()
- .Consume()

**Control**
- .Play()
- .Pause()
- .Stop()
- .Restart()
- .Unlock()

**Special**
- **.If(Func<bool>)** Appends a boolean condition that stops the queue when
  isn't fullfiled. On Repeat mode the queue is restarted. The interruption
  also affects Consume mode (no execution, no removal).

**Destructive**
- .Reset()
