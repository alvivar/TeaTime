- **BUG FIXED**: Sometimes, on concurrent environments, AppendLoop with a
  manual Break() used to append wrong his next chained callback.

- Minor optimizations.

####v0.5.1

- **NEW**: ttHandler now supports Append.

- **NEW**: ttHandler 'WaitFor(', waits for a yield or time after the current
  callback execution, just before the next queued callback.

####v0.5

- **NEW**: AppendLoop, appends a callback that runs frame by frame for his
  duration (or infinite) into a queue.

- **NEW**: ttHandler, supports and manages AppendLoop with 'timeSinceStart'
  and 'Break()'.

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