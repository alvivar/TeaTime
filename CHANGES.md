####v0.7.3 beta
#####2015/12/14

- **NEW**: **ttHandler** now holds a reference to itselfs (**.self**);

####v0.7.2 beta
#####2015/10/19

- **NEW**: .If(Func<bool>) Appends a boolean condition that stops the queue
  when isn't fullfiled. On Repeat mode the queue is restarted. The
  interruption also affects Consume mode (no execution, no removal).

- **CHANGE**: .Repeat() mode no longer ignores new appends. That's exclusive
  to .Wait().

####v0.7 beta
#####2015/10/10

- Total code rewrite. The same pattern but faster, **C#** compliant, cleaner,
  better!

- **CHANGE**: TeaTime is a normal instantiable Object now i.e: **TeaTime queue
  = new TeaTime(MonoBehaviour);**

- **CHANGE**: **this.tt()** (MonoBehaviour extension) returns a new TeaTime
  queue ready to be used.

- **CHANGE**: **this.tt("queueName")** returns a TeaTime queue bounded to his
  name, unique per instance, new on the first call. This allows you to access
  queues without a formal definition (as usual). Dark magic.

- **CHANGE**: Each TeaTime queue handles itself (one coroutine per queue).
  There is no global controls yet, only per queue.

- **OPTIMIZATION**: Callbacks are saved permanently by default.

- **NEW RULE**: Calling **.Add(** or **.Loop(** activates stopped queues even
  during **.Wait()** or **.Repeat()** modes unless they are **.Pause()**.

- **NEW**: **Consume mode**, each callback is removed from the queue after
  execution (non accumulative).

- **NEW**: **.IsPlaying** property, true during execution.

- **NEW**: **.IsCompleted** property, true if the queue execution is done.

- **NEW**: Queue **.Count** property.

- **NEW**: **.Current** property, current queue position to be executed.

####v0.6.5.4

- .ttPlay is now able to restart a queue if it's already finished.

####v0.6.5.3

- **UPGRADE** .WaitFor() from ttHandler now queues his arguments every time is
  called, it also executes and waits IEnumerators.

- **UPGRADE** .ttAdd() now supports single YieldInstructions.

- Minor optimizations.

####v0.6.5.1:

- **BUG FIXED**: Using a TeaTime queue inside a TeaTime queue used to cause a
  problem with the queue name reference. It's fixed now.

####v0.6.5:

- **CHANGE**: .ttRepeat default parameter changed to -1 (n = -1) for infinite
  repetition (used to be 1).

- **NEW**: ttPause(, ttStop( and ttPlay( are done and they work exactly as you
  would think.

- **NEW**: Play, Pause, Stop example.

- Minor optimizations & code cleanup.

####v0.6.2

- **NEW**: ttReset is back, it stops and resets the current queue.

- **CHANGE**: tt( optional parameter can't reset a queue anymore (use ttReset
  after tt( instead).

- Minor optimizations.

- Deprecated code cleanup.

- Updated examples.

####v0.6

- **NEW**: .ttRepeat(n), repeats the current queue n times or infinite (n <=
  -1).

- **CHANGE**: ttAdd( & ttLoop(, can't create/change the current queue now, use
  tt( instead.

- **NEW**: Better examples!

####v0.5.9

- **CHANGE**: ttNew( upgraded to tt( and now is able to change the current
  queue. It can also reset the content of an existent queue (optional). When
  used without name, the queue will be anonymous (i.e. immune to ttWait).

####v0.5.8.4

- **NEW**: ttNew( create or change a queue the current queue, using an unique
  anonymous identifier.

- **CHANGE**: ttNow( does not exists anymore, you can create fast & safe
  timers using ttNew( at the beginning of the queue (both ttAdd( & ttLoop().

####v0.5.8.3

- **CHANGE**: ttReset( upgraded to TeaTime.Reset(, able to stop and reset
  queues.

- **NEW**: TeaTime.ResetAll( stop and clean all queues in all instances.

####v0.5.8

- **NEW**: ttHandler.t has returned. It contains the completion percentage
  expresed from 0 to 1 for timed loops (i.e. On half duration t = 0.5).

####v0.5.7

- **NEW**: ttReset( stops and resets a queue.

- **CHANGE**: ttWaitForCompletion( renamed to ttWait(.

####v0.5.4

- **CHANGE**: ttHandler.t renamed to ttHandler.deltaTime (algorithm greatly
  improved!).

####v0.5.2

- **CHANGE**: ttAppend( renamed to ttAdd(.

- **CHANGE**: ttAppendLoop( renamed to ttLoop(.

- **CHANGE**: ttInvoke( renamed to ttNow(.

- **CHANGE**: ttLock( renamed to ttWaitForCompletion(.

####v0.5.1

- TeaTimer was renamed to TeaTime! :D +1

- **BUG FIXED**: On concurrent environments, AppendLoop with a manual Break()
  used to append wrong his next chained callback.

- Minor optimizations.

####v0.5

- **NEW**: ttHandler now supports Append(.

- **NEW**: ttHandler 'WaitFor(', waits for a yield or time after the current
  callback execution, just before the next queued callback.

####v0.4

- **NEW**: AppendLoop(, appends a callback that runs frame by frame for his
  duration (or infinite) into a queue.

- **NEW**: ttHandler, supports and manage AppendLoop with 't' (completion rate
  from 0 to 1), 'timeSinceStart' and 'Break()'.

- **CHANGE**: ttNow( renamed to ttInvoke.

####v0.3

- **NEW**: ttLock(, locks the current queue until all his previous callbacks
  are done (safe to run during arbitrary cycles e.g. Update()).

- **CHANGE**: ttInsert( renamed to ttNow.

####v0.2

- **NEW**: ttAppend( can name queues (different queues can coexist at the same
  time).

####v0.1

- **NEW**: ttAppend(, appends a timed callback into a queue.

- **NEW**: ttInsert(, executes a timed callback.
