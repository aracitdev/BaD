# BaD--
 A very bad compiled programmig language.

 A strongly typed lisp like language using L-expressions, compiling to x86_64 assembly targetting the FASM assembler.

 BaD-- follows the standard C style calling conventions, allowing for it to call external functions from dlls. This functionality is provided through the extern feature. Automatically allocates stack space for constants including strings, and can allow for heap allocation through external libraries. Return types, standard control statmeents (if, while, blocks), and pointer operations are also supported. 

 Example code:
```
(extern MessageBox (int (*char) (*char) int) int)
 (func main () void
  (vardec (*char) title)
  (vardec (*char) message)
  (assign title ""Hello World"")
  (assign message ""Hello from BaD"")
  (call MessageBox 0 title message 0)
))
```

 [Language Documentation](https://github.com/aracitdev/BaD/blob/main/BaD--%20Documentation.pdf)
