"""Local-only server for the threaded Godot/.NET browser export."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import re


class GameHandler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, '.wasm': 'application/wasm', '.pck': 'application/octet-stream'}

    def send_head(self):
        source = Path(self.translate_path(self.path))
        compressed = Path(str(source) + '.gz')
        encoding = self.headers.get('Accept-Encoding', '')
        accepts_gzip = re.search(r'(?:^|,)\s*gzip\s*(?:;\s*q=([\d.]+))?(?=\s*(?:,|$))', encoding)
        if source.suffix in {'.wasm', '.pck', '.js'} and compressed.is_file() and accepts_gzip and float(accepts_gzip[1] or '1') > 0:
            stream = compressed.open('rb')
            self.send_response(200)
            self.send_header('Content-Type', self.guess_type(str(source)))
            self.send_header('Content-Encoding', 'gzip')
            self.send_header('Vary', 'Accept-Encoding')
            self.send_header('Content-Length', str(compressed.stat().st_size))
            self.end_headers()
            return stream
        return super().send_head()

    def end_headers(self):
        self.send_header('X-Parking-Practice', 'web-prototype')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        self.send_header('Cross-Origin-Resource-Policy', 'same-origin')
        self.send_header('Cache-Control', 'no-cache')
        super().end_headers()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=8060)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1] / 'builds' / 'web')
    args = parser.parse_args()
    if not (args.root / 'index.html').is_file():
        parser.error('Build the web export first with build-web.ps1.')
    server = ThreadingHTTPServer(('127.0.0.1', args.port), partial(GameHandler, directory=str(args.root.resolve())))
    print(f'Parking Practice: http://localhost:{args.port}', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
