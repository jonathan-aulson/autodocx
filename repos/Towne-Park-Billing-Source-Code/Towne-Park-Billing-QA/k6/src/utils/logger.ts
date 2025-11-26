export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3
}

export interface LogEntry {
  level: LogLevel;
  message: string;
  data?: any;
  timestamp: string;
  testId?: string;
}

export class Logger {
  private readonly enableDetailedLogs: boolean;
  private readonly testId: string;

  constructor(enableDetailedLogs: boolean = false, testId?: string) {
    this.enableDetailedLogs = enableDetailedLogs;
    this.testId = testId || this.generateTestId();
  }

  private generateTestId(): string {
    return `test_${Date.now()}_${Math.random().toString(36).substring(7)}`;
  }

  private formatMessage(level: LogLevel, message: string, data?: any): string {
    const timestamp = new Date().toISOString();
    const levelName = LogLevel[level];
    const baseMessage = `[${timestamp}] [${levelName}] [${this.testId}] ${message}`;

    if (data && Object.keys(data).length > 0) {
      return `${baseMessage} ${JSON.stringify(data)}`;
    }

    return baseMessage;
  }

  debug(message: string, data?: any): void {
    if (this.enableDetailedLogs) {
      console.log(this.formatMessage(LogLevel.DEBUG, message, data));
    }
  }

  info(message: string, data?: any): void {
    console.log(this.formatMessage(LogLevel.INFO, message, data));
  }

  warn(message: string, data?: any): void {
    console.warn(this.formatMessage(LogLevel.WARN, message, data));
  }

  error(message: string, data?: any): void {
    console.error(this.formatMessage(LogLevel.ERROR, message, data));
  }

  createChildLogger(suffix: string): Logger {
    return new Logger(this.enableDetailedLogs, `${this.testId}_${suffix}`);
  }
}