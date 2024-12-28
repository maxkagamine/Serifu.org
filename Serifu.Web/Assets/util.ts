export function assertDefined<T>(value: T, name?: string): NonNullable<T> {
  if (value == null) {
    throw new Error(`Assertion failed: expected ${name ?? 'value'} to be defined`);
  }
  return value;
}
