"""Generate OpenAPI schema artifact."""

import json
from pathlib import Path

from app.main import create_app


def main() -> None:
    app = create_app()
    schema = app.openapi()
    output_path = Path(__file__).resolve().parents[1] / "openapi.json"
    output_path.write_text(json.dumps(schema, indent=2), encoding="utf-8")
    print(f"Wrote OpenAPI schema to {output_path}")


if __name__ == "__main__":
    main()
