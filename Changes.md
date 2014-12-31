- NEW: AppendLoop, appends a callback that runs frame by frame for his
  duration into a queue.

- CHANGE: ttNow renamed to ttInvoke.

v0.4

- NEW: ttLock, locks the current queue until all his previous callbacks are
  done (safe to run during arbitrary cycles e.g. Update()).

- CHANGE: ttInsert renamed to ttNow.

v0.3

- NEW: ttAppend can name queues (different queues can coexist at the same
  time).

v0.2

- NEW: ttAppend, appends a timed callback into a queue.

- NEW: ttInsert, executes a timed callback.

v0.1