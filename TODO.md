# To do

- this.tt() should be cacheable when ends
- this.tt() should be collected so it can be handled by the manager

- More and better examples
- A decent stress test
- A TeaTime manager that runs everything
- Pool cache support ^
- Improve time precision ^
- Use arrays instead of generic lists ^
- A way to allow parallel teatimes (.Now?)
- The waitingList bug when a TeaTime waits another TeaTime

# Bugs

- Wait doesn't work after a add(1) when it's waiting a tt("something") teatime with reset?

# Done

- Yoyo mode (Forward, Backward cycle)
- A good testing to Reverse/Backward/Forward mode
- Rewrite the main algorithm to optimize coroutine instantiation
- Replace Foreachs by For
- TeaTime should be able to wait other TeaTimes
- Lots of things before starting this to-do
