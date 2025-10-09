#!/usr/bin/env python3
"""
Quick test to verify that the stty command validation fix works correctly.
This script simulates the stty commands that should now pass validation.
"""

# Example stty commands that should be VALID (not blocked)
valid_commands = [
    "stty -F /dev/ttyUSB0 cs8 9600 -ignbrk brkint -icrnl imaxbel ixon opost onlcr isig icanon iexten echo echoe echok echoctl echoke crtscts -parodd -parenb",
    "stty -F /dev/ttyACM0 cs7 115200 raw",
    "stty -F /dev/ttyS0 cs8 38400 -echo",
    "stty -F /dev/ttyUSB1 cs8 9600 parenb parodd",  # Contains "dd" but should be valid
]

# Example commands that should be INVALID (blocked)
invalid_commands = [
    "stty -F /dev/ttyUSB0 cs8 9600; dd if=/dev/zero of=/dev/sda",  # dangerous dd after semicolon
    "stty -F /dev/ttyUSB0 cs8 9600 && dd if=/dev/zero of=/dev/sda",  # dangerous dd after &&
    "stty -F /dev/ttyUSB0 cs8 9600 | dd if=/dev/zero of=/dev/sda",  # dangerous dd after pipe
    "dd if=/dev/zero of=/dev/sda",  # standalone dd command
    "stty -F /dev/ttyUSB0 cs8 9600; rm -rf /",  # dangerous rm command
]

print("=== STTY Command Validation Test ===\n")

print("Commands that SHOULD BE VALID:")
for i, cmd in enumerate(valid_commands, 1):
    print(f"{i}. {cmd}")

print("\nCommands that SHOULD BE BLOCKED:")
for i, cmd in enumerate(invalid_commands, 1):
    print(f"{i}. {cmd}")

print("\n=== Test Pattern Matching ===")
import re

# Test the dangerous patterns from the fixed code
dangerous_patterns = [
    r"rm\s+", r"del\s+", r"format\s+", r"mkfs\s+",
    r";\s*dd\s+", r"&&\s*dd\s+", r"\|\s*dd\s+", r"^\s*dd\s+",  # Only dangerous dd usage
    r">\s*/dev/", r";\s*rm\s+", r"&&\s*rm\s+", r"\|\s*rm\s+"
]

def test_command_validation(command):
    """Test if a command would be blocked by the validation patterns"""
    for pattern in dangerous_patterns:
        if re.search(pattern, command, re.IGNORECASE):
            return False, f"Blocked by pattern: {pattern}"
    return True, "Valid"

print("\nTesting valid commands:")
for cmd in valid_commands:
    is_valid, reason = test_command_validation(cmd)
    status = "✅ PASS" if is_valid else "❌ FAIL"
    print(f"{status}: {cmd}")
    if not is_valid:
        print(f"    Reason: {reason}")

print("\nTesting invalid commands:")
for cmd in invalid_commands:
    is_valid, reason = test_command_validation(cmd)
    status = "✅ PASS" if not is_valid else "❌ FAIL"  # Inverted logic for invalid commands
    print(f"{status}: {cmd}")
    if not is_valid:
        print(f"    Correctly blocked by: {reason}")
    else:
        print(f"    ⚠️  WARNING: This dangerous command was not blocked!")

print("\n=== Test Complete ===")
