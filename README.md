# DeskConcierge

Tool for household mail. Photograph or scan a letter — it runs OCR, pulls the obvious
fields with regex, lets a local LLM read the rest (who sent it, what it's about, any
deadlines), and files the original into a folder structure you can actually navigate.
Files stay the source of truth, the database is just a throwaway index.


## how it works

`ingest → OCR → heuristics → LLM → archive → happy user`

- **ingest** — upload, hash, dedup, drop in `inbox/`
- **OCR** — Tesseract (CLI), German + English, confidence per word
- **heuristics** — regex for IBAN (mod-97 checked), date, amount, invoice number, each with a confidence
- **LLM** — local Ollama reads the OCR text and returns sender / type / summary / dates / action-needed as forced JSON
- **archive** — original moves to `store/{year}/{sender}/{date}_{slug}_{ref}.ext`, with a JSON sidecar next to it


## running it

you need:

- .NET 9 SDK
- Tesseract — `brew install tesseract tesseract-lang`
- Ollama (the app, **not** the brew formula — that one ships without the runner) — `brew install --cask ollama`, then `ollama pull gemma3:12b`

then:

```
dotnet run --project DeskConcierge.Api
```

open <http://localhost:5196> and drop in an image. the model is set in
`DeskConcierge.Api/appsettings.json` under `Llm` — switch it by uncommenting another line.

## tests

```
dotnet test
```

xUnit. no running model needed, the LLM and OCR are faked.

## todo

- [ ] "view original" + full-text toggle in the frontend
- [ ] PDF input (images only for now)
- [ ] reprocessing — re-run the pipeline on already-stored docs without re-uploading (for when the heuristics or model get better)
- [ ] image preprocessing — straighten/crop a crooked phone photo into a clean scan 
