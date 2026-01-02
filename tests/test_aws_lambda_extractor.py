from pathlib import Path

from autodocx.extractors.aws_lambda import AWSLambdaExtractor


def test_lambda_extractor_parses_sam_template(tmp_path: Path) -> None:
    template = tmp_path / "template.yaml"
    template.write_text(
        """
Resources:
  MyFunction:
    Type: AWS::Lambda::Function
    Properties:
      Runtime: python3.11
      Handler: app.handler
      Timeout: 30
      MemorySize: 512
      Events:
        ApiEvent:
          Type: Api
          Properties:
            Path: /hello
            Method: get
        CronEvent:
          Type: Schedule
          Properties:
            Schedule: rate(5 minutes)
        FileUpload:
          Type: S3
          Properties:
            Bucket: inbound-files
""",
        encoding="utf-8",
    )
    extractor = AWSLambdaExtractor()
    signals = list(extractor.extract(template))
    assert len(signals) == 1
    signal = signals[0]
    assert signal.props["runtime"] == "python3.11"
    assert signal.props["handler"] == "app.handler"
    assert len(signal.props["triggers"]) == 3
    assert signal.props["relationships"][0]["target"]["display"] == "GET /hello"
    datastores = signal.props.get("datasource_tables") or []
    assert "inbound-files" in datastores
    services = signal.props.get("service_dependencies") or []
    assert "GET /hello" in services
    assert signal.props.get("steps"), "steps should summarize event bindings"


def test_lambda_extractor_parses_serverless_manifest(tmp_path: Path) -> None:
    manifest = tmp_path / "serverless.yml"
    manifest.write_text(
        """
service: demo
provider:
  name: aws
functions:
  hello:
    handler: src/handler.hello
    runtime: nodejs18.x
    events:
      - http:
          method: post
          path: submit
      - schedule: rate(1 hour)
      - s3:
          bucket: uploads
""",
        encoding="utf-8",
    )
    extractor = AWSLambdaExtractor()
    signals = list(extractor.extract(manifest))
    assert len(signals) == 1
    signal = signals[0]
    assert signal.props["name"] == "hello"
    assert signal.props["runtime"] == "nodejs18.x"
    assert len(signal.props["relationships"]) >= 2
    assert "uploads" in (signal.props.get("datasource_tables") or [])
    assert signal.props.get("service_dependencies"), "service dependencies should include HTTP trigger"
