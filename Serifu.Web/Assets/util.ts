export function assertDefined<T>(value: T, name: string): NonNullable<T> {
  if (value == null) {
    throw new Error(`Assertion failed: ${name} should be defined`);
  }
  return value;
}
