workspace "SUI MAIS Workspace" "This workspace/repository contains software that demonstrates the viability of using the NHS number as the SUI and how data could be transferred between data owners nationally, to present this data as a single view of a child for the improved safeguarding of children." {

    !identifiers hierarchical

    model {
        u = person "User"
        ss = softwareSystem "Software System" {
            wa = container "Web Application"
            db = container "Database Schema" {
                tags "Database"
            }

            // !adrs decisions
        }

        u -> ss.wa "Uses"
        ss.wa -> ss.db "Reads from and writes to"
    }

    views {
        systemContext ss "Diagram1" {
            include *
            autolayout lr
        }

        container ss "Diagram2" {
            include *
            autolayout lr
        }

        styles {
            element "Element" {
                color #f88728
                stroke #f88728
                strokeWidth 7
                shape roundedbox
            }
            element "Person" {
                shape person
            }
            element "Database" {
                shape cylinder
            }
            element "Boundary" {
                strokeWidth 5
            }
            relationship "Relationship" {
                thickness 4
            }
        }
    }

    // !docs content

    configuration {
        scope softwaresystem
    }

}