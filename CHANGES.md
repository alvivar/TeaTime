- **NEW**: ttHandler.t has returned. It contains the completion percentage
  expresed from 0 to 1 for timed loops (i.e. On half duration t = 0.5).

####v0.5.8

- **NEW**: ttReset stops and resets a queue.

- **CHANGE**: ttWaitForCompletion renamed to ttWait.

####v0.5.7

- **CHANGE**: ttHandler.t renamed to ttHandler.deltaTime. Also, his algorithm
  was greatly improved.

####v0.5.4

- **CHANGE**: ttAppend renamed to ttAdd.

- **CHANGE**: ttAppendLoop renamed to ttLoop.

- **CHANGE**: ttInvoke renamed to ttNow.

- **CHANGE**: ttLock renamed to ttWaitForCompletion.

####v0.5.2

- TeaTimer was renamed to TeaTime! :D +1

- **BUG FIXED**: On concurrent environments, AppendLoop with a manual Break()
  used to append wrong his next chained callback.

- Minor optimizations.

####v0.5.1

- **NEW**: ttHandler now supports Append.

- **NEW**: ttHandler 'WaitFor(', waits for a yield or time after the current
  callback execution, just before the next queued callback.

####v0.5

- **NEW**: AppendLoop, appends a callback that runs frame by frame for his
  duration (or infinite) into a queue.

- **NEW**: ttHandler, supports and manage AppendLoop with 't' (completion rate
  from 0 to 1), 'timeSinceStart' and 'Break()'.

- **CHANGE**: ttNow renamed to ttInvoke.

####v0.4

- **NEW**: ttLock, locks the current queue until all his previous callbacks
  are done (safe to run during arbitrary cycles e.g. Update()).

- **CHANGE**: ttInsert renamed to ttNow.

####v0.3

- **NEW**: ttAppend can name queues (different queues can coexist at the same
  time).

####v0.2

- **NEW**: ttAppend, appends a timed callback into a queue.

- **NEW**: ttInsert, executes a timed callback.

####v0.1
