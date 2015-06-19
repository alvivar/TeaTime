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
