import urllib.request
import re
import json

url = 'https://gemini.google.com/share/6b401f09beb7'
try:
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    html = urllib.request.urlopen(req).read().decode('utf-8')

    # The text is usually within a specific script tag
    match = re.search(r'AF_initDataCallback\(\{key: \'ds:1\',.*?data: (.*?)\}\);</script>', html, re.DOTALL)
    if match:
        data_str = match.group(1)
        # It's not pure JSON, it's JS literal arrays. Let's try to extract string literals
        strings = re.findall(r'"((?:[^"\\]|\\.)*)"', data_str)
        # Filter for the long ones that contain the conversation
        for s in strings:
            if 'Auditor' in s and len(s) > 100:
                print("\n=== Found Content ===\n")
                # Need to manually decode unicode and unescape
                decoded = s.encode('utf-8').decode('unicode_escape')
                # Also replace \n
                decoded = decoded.replace('\\n', '\n')
                print(decoded)
                break
except Exception as e:
    print(e)
