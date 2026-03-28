import urllib.request
import re

url = 'https://g.co/gemini/share/6b401f09beb7'
try:
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'})
    html = urllib.request.urlopen(req).read().decode('utf-8')

    with open('gemini_full.html', 'w') as f:
        f.write(html)

    print("Downloaded length:", len(html))

    import json

    # Try finding large JSON blocks
    blocks = re.findall(r'AF_initDataCallback\((.*?)\);</script>', html, re.DOTALL)
    for i, b in enumerate(blocks):
        if 'Auditor' in b or 'CVE' in b or 'Siemens' in b or 'efargas/S7-Tools' in b:
            print(f"Found keyword in block {i}")
            # Do a regex search for the text snippet
            match = re.search(r'efargas/S7-Tools', b)
            if match:
                print("Found S7-Tools reference")

except Exception as e:
    print(e)
