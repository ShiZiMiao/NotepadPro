import sys

def greet(name: str) -> str:
    # say hello
    msg = f"Hello, {name}!"
    return msg

if __name__ == "__main__":
    print(greet("NotepadPro"))
    print(42, 3.14, True)
