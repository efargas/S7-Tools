import urllib.request
import re

url = 'https://gemini.google.com/share/6b401f09beb7'
try:
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    html = urllib.request.urlopen(req).read().decode('utf-8')

    # Let's just find anything containing "Auditoría" and print the next 2000 chars
    start_idx = html.find('Auditoría de Seguridad')
    if start_idx != -1:
        snippet = html[start_idx:start_idx+8000]
        # Clean it up from weird JSON/array formatting
        snippet = re.sub(r'\\[rn]', '\n', snippet)
        snippet = snippet.replace('\\"', '"')
        print(snippet[:2000])
    else:
        print("Not found")

except Exception as e:
    print(e)
