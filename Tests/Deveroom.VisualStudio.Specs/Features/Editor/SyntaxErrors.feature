﻿Feature: Syntax errors

Rules:

* Highlights parser errors
	* Highlights syntax errors
	* Highlights semantic errors

Scenario: Highlights syntax errors
	Given there is a SpecFlow project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			NoSuchKeyword something else
			When I press multiply
			Then a step with an unfinished doc string
				```
				doc string
		"""
	Then all ParserError section should be highlighted as
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			{ParserError}NoSuchKeyword something else{/ParserError}
			When I press multiply
			Then a step with an unfinished doc string
				```
		{ParserError}		doc string{/ParserError}
		"""

Scenario: Highlights semantic errors
	Given there is a SpecFlow project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			Then a step with a bad data table
				| foo |
				| bar | baz |

		Scenario: Add two numbers
			When I press add
		"""
	Then all ParserError section should be highlighted as
		"""
		Feature: Addition

		Scenario: Add two numbers
			When I press add
			Then a step with a bad data table
				| foo |
				{ParserError}| bar | baz |{/ParserError}

		{ParserError}Scenario: Add two numbers{/ParserError}
			When I press add
		"""

Scenario: Highlights top of the file on "Unexpected end of file" syntax error
	Given there is a SpecFlow project scope
	When the following feature file is opened in the editor
		"""
		Feature: Addition
		@tag
		"""
	Then all DefinitionLineKeyword section should be highlighted as
		"""
		{DefinitionLineKeyword}Feature:{/DefinitionLineKeyword} Addition
		@tag
		"""
